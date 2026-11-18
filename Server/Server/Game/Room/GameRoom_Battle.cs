using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.Collections;
using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game.Room
{
    // 전투는 라운드 단위로 진행한다. 두 사람이 행동을 동시에 고르면 서버가 우선도·스피드 순으로 처리한다.
    //  Choosing  : 두 사람의 행동(C_Turn)을 기다린다.
    //  Animating : 결과를 보냈고, 두 클라이언트의 연출 종료(C_TurnEnd)를 기다린다.
    //  Replacing : 쓰러진 포켓몬 대신 낼 포켓몬(C_ChangePokemon)을 기다린다.
    public enum BattlePhase
    {
        Choosing,
        Animating,
        Replacing
    }

    public partial class GameRoom
    {
        private const int confusionSkillId = 999;

        // 교체는 어떤 기술보다 먼저 일어난다.
        private const int SwitchPriority = 7;
        private const int ParalysisFullChance = 25;
        private const int ConfusionSelfHitChance = 33;

        public BattlePhase Phase { get; private set; } = BattlePhase.Choosing;

        // 이번 라운드에 고른 행동
        private readonly Dictionary<int, TurnInfo> _chosenActions = new Dictionary<int, TurnInfo>();

        // 이번 라운드에 이미 행동한 플레이어. 풀죽음은 아직 행동하지 않은 상대에게만 걸린다.
        private readonly HashSet<int> _actedThisRound = new HashSet<int>();

        // 쓰러진 포켓몬을 바꿔야 하는 플레이어 → 고른 포켓몬(아직이면 -1)
        private readonly Dictionary<int, int> _pendingReplacements = new Dictionary<int, int>();

        // 순풍이 남은 턴 수(플레이어 단위)
        private readonly Dictionary<int, int> _tailwindTurns = new Dictionary<int, int>();

        // 묶인 플레이어 → 묶은 기술, 묶은 플레이어, 묶은 포켓몬. 묶은 포켓몬이 빠지면 풀린다.
        private readonly Dictionary<int, (int skillId, int trapperPlayerId, int trapperPokemonId)> _binds =
            new Dictionary<int, (int, int, int)>();

        // (플레이어 ID, 포켓몬 ID) → 그 포켓몬에 씨앗을 심은 기술. 데미지 문구에 기술 이름을 쓴다.
        private readonly Dictionary<(int playerId, int pokemonId), int> _dotSkillIds = new Dictionary<(int, int), int>();

        // (플레이어, 포켓몬, 기술) → 남은 PP
        private readonly Dictionary<(int playerId, int pokemonId, int skillId), int> _pp =
            new Dictionary<(int, int, int), int>();

        // 고른 포켓몬이 정해진 뒤 부른다. 기술마다 데이터의 PP로 채운다.
        private void InitPP()
        {
            _pp.Clear();
            foreach (Player player in _players.Values)
                foreach (PokemonData pokemon in player.Pokemon)
                    foreach (int skillId in pokemon.Info.SkillId)
                        _pp[(player.Id, pokemon.Id, skillId)] = DataManager.SkillDict[skillId].info.Pp;
        }

        public int RemainingPP(int playerId, int pokemonId, int skillId)
        {
            return _pp.TryGetValue((playerId, pokemonId, skillId), out int pp) ? pp : 0;
        }

        private void ResetBattleState()
        {
            Phase = BattlePhase.Choosing;
            _chosenActions.Clear();
            _actedThisRound.Clear();
            _pendingReplacements.Clear();
            _tailwindTurns.Clear();
            _binds.Clear();
            _dotSkillIds.Clear();
        }

        // playerId는 세션에서 얻은 행위자다. 패킷의 PlayerId는 쓰지 않는다.
        public void Turn(int playerId, C_Turn packet)
        {
            if (!_battleStarted || Phase != BattlePhase.Choosing)
                return;

            if (packet.TurnInfo == null || _players.Count != (int)TargetType.End)
                return;

            // 한 라운드에 행동은 한 번만 받는다.
            if (!_players.TryGetValue(playerId, out Player player) || _chosenActions.ContainsKey(playerId))
                return;

            TurnInfo action = packet.TurnInfo;
            switch (action.Action)
            {
                case ActionType.Runaway:
                    // 항복은 상대를 기다리지 않고 바로 전투를 끝낸다.
                    DuelEnd(playerId, true);
                    return;
                case ActionType.Fight:
                    if (!CanUseSkill(playerId, action.SkillId))
                        return;
                    break;
                case ActionType.Change:
                    // 묶인 포켓몬은 스스로 교체할 수 없다.
                    if (!CanSwitchTo(player, action.ChangePokemonId) || _binds.ContainsKey(playerId))
                        return;
                    break;
                case ActionType.Pass:
                    break;
                default:
                    return;
            }

            _chosenActions[playerId] = action.Clone();
            if (_chosenActions.Count == _players.Count)
                ResolveRound();
        }

        // 스킬이 데이터에 있고, 지금 나와 있는 포켓몬이 가진 스킬이고, PP가 남아 있어야 한다.
        private bool CanUseSkill(int playerId, int skillId)
        {
            return DataManager.SkillDict.ContainsKey(skillId)
                   && _players.TryGetValue(playerId, out Player player)
                   && player.Pokemon.Count > 0
                   && player.Pokemon[0].Info.SkillId.Contains(skillId)
                   && RemainingPP(playerId, player.Pokemon[0].Id, skillId) > 0;
        }

        // 자기 목록에 있고, 지금 나와 있지 않고, 쓰러지지 않은 포켓몬으로만 바꿀 수 있다.
        private static bool CanSwitchTo(Player player, int pokemonId)
        {
            int index = player.Pokemon.FindIndex(pokemon => pokemon.Id == pokemonId);
            return index > 0 && player.Pokemon[index].Info.Hp > 0;
        }

        private void ResolveRound()
        {
            Phase = BattlePhase.Animating;
            foreach (int id in _players.Keys.ToList())
                _playerReady[id] = false;
            _actedThisRound.Clear();

            foreach (int playerId in OrderActions())
            {
                // 앞 행동으로 전투가 끝났으면 나머지는 처리하지 않는다.
                if (!_battleStarted)
                    return;

                TurnInfo action = _chosenActions[playerId];
                switch (action.Action)
                {
                    case ActionType.Change:
                        SwitchIn(playerId, action.ChangePokemonId);
                        break;
                    case ActionType.Fight:
                        // 차례가 오기 전에 쓰러졌으면 기술을 쓰지 못한다.
                        if (_players[playerId].Pokemon[0].Info.Hp > 0)
                            Fight(playerId, action.SkillId);
                        break;
                }

                _actedThisRound.Add(playerId);
            }

            _chosenActions.Clear();

            if (_battleStarted)
                EndOfRound();
        }

        // 교체 → 기술 우선도 → 스피드 순. 모두 같으면 무작위다.
        private List<int> OrderActions()
        {
            return _chosenActions.Keys
                .OrderByDescending(id => ActionPriority(_chosenActions[id]))
                .ThenByDescending(EffectiveSpeed)
                .ThenBy(_ => Random.Shared.Next())
                .ToList();
        }

        private static int ActionPriority(TurnInfo action)
        {
            if (action.Action == ActionType.Change)
                return SwitchPriority;
            if (action.Action == ActionType.Fight)
                return DataManager.SkillDict[action.SkillId].info.Priority;
            return 0;
        }

        // 순풍이 불면 2배, 마비면 절반이다.
        public int EffectiveSpeed(int playerId)
        {
            PokemonInfo info = _players[playerId].Pokemon[0].Info;
            double speed = info.Spe;
            if (_tailwindTurns.ContainsKey(playerId))
                speed *= 2;
            if (info.State != null && info.State.Paralysis > 0)
                speed /= 2;
            return (int)speed;
        }

        private void Fight(int playerId, int skillId)
        {
            int enemyId = FindEnemyIdByMyId(playerId);
            PokemonData me = _players[playerId].Pokemon[0];
            PokemonData foe = _players[enemyId].Pokemon[0];

            S_TurnBattle battlePacket = new S_TurnBattle { PlayerId = playerId, SkillId = skillId };

            // 풀죽음·마비·혼란 자해로 기술이 나가지 않으면 PP도 줄지 않는다.
            bool canMove = BeforeMove(playerId, me, battlePacket.TurnInfo);
            if (canMove)
            {
                _pp[(playerId, me.Id, skillId)]--;
                UseSkill(playerId, enemyId, me, foe, skillId, battlePacket.Info);
            }

            BattleInfo duelEnd = CheckDuelEnd(out int loserPlayerId);
            if (duelEnd != null)
                (canMove ? battlePacket.Info : battlePacket.TurnInfo).Add(duelEnd);

            Broadcast(battlePacket);

            if (duelEnd != null)
                DuelEnd(loserPlayerId, false);
        }

        // 기술을 쓰기 전의 상태 확인. 풀죽음 → 혼란 → 마비 순이다. 움직일 수 없으면 false.
        private bool BeforeMove(int playerId, PokemonData me, RepeatedField<BattleInfo> results)
        {
            ConditionAbnormality state = me.Info.State;

            if (state.Sturn > 0)
            {
                state.Sturn = 0;
                results.Add(SelfInfo(playerId, me, ApplyType.Sturn, BattleStateFlag.DebuffSturn));
                return false;
            }

            if (state.Confusion > 0)
            {
                state.Confusion--;
                if (state.Confusion == 0)
                {
                    results.Add(SelfInfo(playerId, me, ApplyType.Confusion, BattleStateFlag.RecoveryConfusion));
                }
                else
                {
                    BattleInfo confused = SelfInfo(playerId, me, ApplyType.Confusion, BattleStateFlag.DebuffConfusion);
                    results.Add(confused);

                    if (Random.Shared.Next(100) < ConfusionSelfHitChance)
                    {
                        foreach (SkillEffect effect in DataManager.SkillDict[confusionSkillId].info.SkillEffect)
                            Util.CalcBattle(me, me, effect);

                        // 클라이언트는 이 항목의 ToData로 자해 뒤의 HP를 그린다.
                        confused.LatingSkillId = confusionSkillId;
                        confused.ToData = me.Clone();
                        return false;
                    }
                }
            }

            if (state.Paralysis > 0 && Random.Shared.Next(100) < ParalysisFullChance)
            {
                results.Add(SelfInfo(playerId, me, ApplyType.Paralysis, BattleStateFlag.DebuffParalysis));
                return false;
            }

            return true;
        }

        private void UseSkill(int playerId, int enemyId, PokemonData me, PokemonData foe, int skillId,
            RepeatedField<BattleInfo> results)
        {
            RepeatedField<SkillEffect> effects = DataManager.SkillDict[skillId].info.SkillEffect;
            int lastDamage = 0;

            for (int effectIndex = 0; effectIndex < effects.Count; ++effectIndex)
            {
                SkillEffect effect = effects[effectIndex];
                bool isAttack = effect.ApplyType == ApplyType.Atk || effect.ApplyType == ApplyType.Spa;

                // 흡수·반동·묶기는 앞선 공격이 피해를 줬을 때만 일어난다.
                bool needsDamage = effect.ApplyType == ApplyType.Recovery || effect.ApplyType == ApplyType.Bind ||
                                   (isAttack && effect.Target == TargetType.Oneself);
                if (needsDamage && lastDamage <= 0)
                    continue;

                if (effect.ApplyType == ApplyType.FaintSelf)
                {
                    results.Add(FaintSelf(playerId, me));
                    continue;
                }

                if (effect.ApplyType == ApplyType.Tailwind)
                {
                    _tailwindTurns[playerId] = effect.Value;
                    results.Add(SelfInfo(playerId, me, ApplyType.Tailwind, BattleStateFlag.Default));
                    continue;
                }

                // 상대에게 거는 부가효과(깨물어부수기의 방어 하락 등)의 accuracy는 발동 확률(%)이다.
                // 발동하지 않거나 상대가 이미 쓰러졌으면 아무 일도 없던 것으로 하고, '빗나갔다'로 보내지 않는다.
                SkillEffect appliedEffect = effect;
                if (effectIndex > 0 && effect.Target == TargetType.Enemy)
                {
                    if (foe.Info.Hp <= 0)
                        continue;
                    if (Random.Shared.Next(100) >= effect.Accuracy)
                        continue;

                    appliedEffect = effect.Clone();
                    appliedEffect.Accuracy = 100;
                }

                switch (effect.ApplyType)
                {
                    case ApplyType.Sturn:
                        // 풀죽음은 이번 라운드에 아직 움직이지 않은 상대만 막는다. 문구는 막힐 때 나온다.
                        if (!_actedThisRound.Contains(enemyId))
                            foe.Info.State.Sturn = 1;
                        continue;
                    case ApplyType.Burn:
                    case ApplyType.Paralysis:
                        if (TryInflictMajorStatus(foe, effect.ApplyType))
                            results.Add(EnemyInfo(playerId, enemyId, me, foe, effect.ApplyType));
                        continue;
                    case ApplyType.Bind:
                        if (foe.Info.Hp <= 0 || foe.Info.State.Bind > 0)
                            continue;
                        // 4~5턴 동안 묶는다.
                        foe.Info.State.Bind = Random.Shared.Next(4, 6);
                        _binds[enemyId] = (skillId, playerId, me.Id);
                        BattleInfo bindInfo = EnemyInfo(playerId, enemyId, me, foe, ApplyType.Bind);
                        bindInfo.LatingSkillId = skillId;
                        results.Add(bindInfo);
                        continue;
                }

                int foeHpBefore = foe.Info.Hp;
                BattleInfo battleInfo = Util.CalcBattle(me, foe, appliedEffect, lastDamage);
                battleInfo.FromId = playerId;
                battleInfo.ToId = effect.Target == TargetType.Oneself ? playerId : enemyId;
                results.Add(battleInfo);

                // 빗나간 기술은 나머지 효과를 내지 않는다. 다만 대폭발처럼 쓰기만 해도 쓰러지는 기술은 그대로 쓰러진다.
                bool isMiss = (battleInfo.StateFlag & BattleStateFlag.Miss) == BattleStateFlag.Miss;
                if (isMiss)
                {
                    if (effects.Skip(effectIndex + 1).Any(rest => rest.ApplyType == ApplyType.FaintSelf))
                        results.Add(FaintSelf(playerId, me));
                    break;
                }

                if (isAttack && effect.Target == TargetType.Enemy)
                    lastDamage = foeHpBefore - foe.Info.Hp;

                if (effect.ApplyType == ApplyType.Dot && foe.Info.State.Dot > 0)
                    _dotSkillIds[(enemyId, foe.Id)] = skillId;
            }
        }

        // 화상·마비는 둘 중 하나만 걸린다. 불꽃 타입은 화상, 전기 타입은 마비에 걸리지 않는다.
        private static bool TryInflictMajorStatus(PokemonData target, ApplyType status)
        {
            ConditionAbnormality state = target.Info.State;
            if (target.Info.Hp <= 0 || state.Fire > 0 || state.Paralysis > 0)
                return false;

            if (status == ApplyType.Burn)
            {
                if (target.Info.Type.Contains(Google.Protobuf.Protocol.Type.Fire))
                    return false;
                state.Fire = 1;
            }
            else
            {
                if (target.Info.Type.Contains(Google.Protobuf.Protocol.Type.Electric))
                    return false;
                state.Paralysis = 1;
            }

            return true;
        }

        private BattleInfo FaintSelf(int playerId, PokemonData me)
        {
            me.Info.Hp = 0;
            return SelfInfo(playerId, me, ApplyType.FaintSelf, BattleStateFlag.Default);
        }

        // 라운드가 끝날 때의 데미지(화상 → 씨뿌리기 → 묶기)와 순풍·풀죽음 정리
        private void EndOfRound()
        {
            RepeatedField<BattleInfo> results = new RepeatedField<BattleInfo>();

            foreach (int playerId in _players.Keys.OrderByDescending(EffectiveSpeed).ToList())
            {
                int enemyId = FindEnemyIdByMyId(playerId);
                PokemonData me = _players[playerId].Pokemon[0];
                PokemonData foe = _players[enemyId].Pokemon[0];
                ConditionAbnormality state = me.Info.State;
                int maxHp = MaxHp(me);

                if (me.Info.Hp > 0 && state.Fire > 0)
                {
                    Damage(me, Math.Max(1, maxHp / 16));
                    results.Add(SelfInfo(playerId, me, ApplyType.Burn, BattleStateFlag.DebuffFire));
                }

                if (me.Info.Hp > 0 && state.Dot > 0)
                {
                    // 씨뿌리기: 심어진 포켓몬의 최대 HP 1/8을 빼앗아 상대 자리의 포켓몬이 그만큼 회복한다.
                    int seeded = Math.Min(Math.Max(1, maxHp / 8), me.Info.Hp);
                    Damage(me, seeded);
                    state.Dot--;
                    if (foe.Info.Hp > 0)
                        foe.Info.Hp = Math.Min(MaxHp(foe), foe.Info.Hp + seeded);

                    BattleInfo seedInfo = SelfInfo(playerId, me, ApplyType.Dot, BattleStateFlag.DebuffDot);
                    seedInfo.LatingSkillId = _dotSkillIds.TryGetValue((playerId, me.Id), out int seedSkillId) ? seedSkillId : 0;
                    seedInfo.FromId = enemyId;
                    seedInfo.FromData = foe.Clone();
                    results.Add(seedInfo);
                }

                if (_binds.TryGetValue(playerId, out (int skillId, int trapperPlayerId, int trapperPokemonId) bind))
                {
                    // 묶은 포켓몬이 쓰러졌으면 아무 일 없이 풀린다.
                    PokemonData trapper = _players[bind.trapperPlayerId].Pokemon[0];
                    if (trapper.Id != bind.trapperPokemonId || trapper.Info.Hp <= 0 || me.Info.Hp <= 0)
                    {
                        state.Bind = 0;
                        _binds.Remove(playerId);
                    }
                    else
                    {
                        Damage(me, Math.Max(1, maxHp / 8));
                        state.Bind--;
                        BattleInfo bindInfo = SelfInfo(playerId, me, ApplyType.Bind, BattleStateFlag.DebuffBind);
                        bindInfo.LatingSkillId = bind.skillId;
                        if (state.Bind == 0 && me.Info.Hp > 0)
                        {
                            bindInfo.StateFlag |= BattleStateFlag.RecoveryBind;
                            _binds.Remove(playerId);
                        }
                        results.Add(bindInfo);
                    }
                }
            }

            foreach (int playerId in _tailwindTurns.Keys.ToList())
            {
                if (--_tailwindTurns[playerId] > 0)
                    continue;

                _tailwindTurns.Remove(playerId);
                results.Add(SelfInfo(playerId, _players[playerId].Pokemon[0], ApplyType.Tailwind, BattleStateFlag.TailwindEnd));
            }

            // 풀죽음은 그 라운드에만 유효하다.
            foreach (Player player in _players.Values)
                foreach (PokemonData pokemon in player.Pokemon)
                    pokemon.Info.State.Sturn = 0;

            if (results.Count == 0)
                return;

            BattleInfo duelEnd = CheckDuelEnd(out int loserPlayerId);
            if (duelEnd != null)
                results.Add(duelEnd);

            S_TurnBattle residualPacket = new S_TurnBattle();
            residualPacket.TurnInfo.AddRange(results);
            Broadcast(residualPacket);

            if (duelEnd != null)
                DuelEnd(loserPlayerId, false);
        }

        private static int MaxHp(PokemonData pokemon)
        {
            return DataManager.PokemonDict[pokemon.Id].Info.Hp;
        }

        private static void Damage(PokemonData pokemon, int amount)
        {
            pokemon.Info.Hp = Math.Max(0, pokemon.Info.Hp - amount);
        }

        private static BattleInfo SelfInfo(int playerId, PokemonData pokemon, ApplyType applyType, BattleStateFlag flag)
        {
            return new BattleInfo
            {
                ApplyType = applyType,
                TargetType = TargetType.Oneself,
                StateFlag = flag,
                FromId = playerId,
                ToId = playerId,
                FromData = pokemon.Clone(),
                ToData = pokemon.Clone()
            };
        }

        private static BattleInfo EnemyInfo(int playerId, int enemyId, PokemonData me, PokemonData foe, ApplyType applyType)
        {
            return new BattleInfo
            {
                ApplyType = applyType,
                TargetType = TargetType.Enemy,
                FromId = playerId,
                ToId = enemyId,
                FromData = me.Clone(),
                ToData = foe.Clone()
            };
        }

        // 남은 포켓몬이 없는 쪽이 있으면 전투 종료 항목을 만든다.
        private BattleInfo CheckDuelEnd(out int loserPlayerId)
        {
            loserPlayerId = -1;
            int winnerPlayerId = -1;
            foreach (Player player in _players.Values)
            {
                if (player.IsRemainPokemonExist())
                    winnerPlayerId = player.Id;
                else if (loserPlayerId == -1)
                    loserPlayerId = player.Id;
            }

            if (loserPlayerId == -1)
                return null;

            return new BattleInfo { StateFlag = BattleStateFlag.DuelEnd, WinPlayerId = winnerPlayerId };
        }

        // loserPlayerId: 도주한 쪽, 또는 남은 포켓몬이 없는 쪽
        public void DuelEnd(int loserPlayerId, bool isRunaway)
        {
            // 끝난 전투에는 턴·교체 패킷을 더 받지 않는다.
            _battleStarted = false;

            foreach (KeyValuePair<int, Player> playerInfo in _players)
            {
                S_DuelEnd duelEndPacket = new S_DuelEnd();
                duelEndPacket.IsWin = playerInfo.Key != loserPlayerId;
                duelEndPacket.IsRunaway = isRunaway;
                playerInfo.Value.Session.Send(duelEndPacket);
                playerInfo.Value.Session.HandleReEnterHandler(playerInfo.Value.Room.RoomId);
            }
        }

        // 두 클라이언트가 연출을 마치면, 쓰러진 포켓몬이 있으면 교체를, 없으면 다음 라운드를 시작한다.
        public void TurnEnd(int playerId)
        {
            if (!_battleStarted || Phase != BattlePhase.Animating)
                return;

            // 이 방에 없는 ID가 준비 목록에 새 항목으로 들어가지 않게 한다.
            if (!_playerReady.ContainsKey(playerId))
                return;

            _playerReady[playerId] = true;
            if (_playerReady.Values.Any(ready => !ready))
                return;

            foreach (Player player in _players.Values)
                if (player.Pokemon[0].Info.Hp <= 0 && player.IsRemainPokemonExist())
                    _pendingReplacements[player.Id] = -1;

            if (_pendingReplacements.Count > 0)
            {
                Phase = BattlePhase.Replacing;
                foreach (Player player in _players.Values)
                {
                    player.Session.Send(new S_Turn
                    {
                        PlayerId = player.Id,
                        MyTurn = false,
                        MustChange = _pendingReplacements.ContainsKey(player.Id)
                    });
                }
                return;
            }

            Phase = BattlePhase.Choosing;
            foreach (Player player in _players.Values)
                player.Session.Send(new S_Turn { PlayerId = player.Id, MyTurn = true });
        }

        // 쓰러진 포켓몬 대신 낼 포켓몬. 두 사람이 모두 고르면 한꺼번에 내보낸다.
        public void ChangePokemon(int playerId, int changePokemonId)
        {
            if (!_battleStarted || Phase != BattlePhase.Replacing)
                return;

            if (!_pendingReplacements.TryGetValue(playerId, out int chosen) || chosen != -1)
                return;

            // 자기 목록에 없는 포켓몬으로의 교체 요청은 무시한다.
            if (!_players.TryGetValue(playerId, out Player player) || !CanSwitchTo(player, changePokemonId))
                return;

            _pendingReplacements[playerId] = changePokemonId;
            if (_pendingReplacements.Values.Any(pokemonId => pokemonId == -1))
                return;

            Phase = BattlePhase.Animating;
            foreach (int id in _players.Keys.ToList())
                _playerReady[id] = false;

            foreach (KeyValuePair<int, int> replacement in _pendingReplacements.ToList())
                SwitchIn(replacement.Key, replacement.Value);
            _pendingReplacements.Clear();
        }

        private void SwitchIn(int playerId, int pokemonId)
        {
            Player player = _players[playerId];
            int index = player.Pokemon.FindIndex(pokemon => pokemon.Id == pokemonId);
            if (index <= 0)
                return;

            ClearOnSwitchOut(playerId, player.Pokemon[0]);
            (player.Pokemon[0], player.Pokemon[index]) = (player.Pokemon[index], player.Pokemon[0]);

            Broadcast(new S_ChangePokemon { PlayerId = playerId, ChangePokemonId = pokemonId });
        }

        // 들어가는 포켓몬의 능력치 변화·씨뿌리기·묶기·혼란·풀죽음은 풀린다. 화상·마비는 남는다.
        private void ClearOnSwitchOut(int playerId, PokemonData outgoing)
        {
            PokemonInfo baseInfo = DataManager.PokemonDict[outgoing.Id].Info;
            outgoing.Info.Atk = baseInfo.Atk;
            outgoing.Info.SpA = baseInfo.SpA;
            outgoing.Info.Def = baseInfo.Def;
            outgoing.Info.SpD = baseInfo.SpD;
            outgoing.Info.Spe = baseInfo.Spe;

            ConditionAbnormality state = outgoing.Info.State;
            state.Dot = 0;
            state.Bind = 0;
            state.Confusion = 0;
            state.Sturn = 0;

            _dotSkillIds.Remove((playerId, outgoing.Id));
            _binds.Remove(playerId);

            // 이 포켓몬이 묶어 둔 상대도 풀린다.
            foreach (int boundPlayerId in _binds.Where(bind => bind.Value.trapperPlayerId == playerId)
                         .Select(bind => bind.Key).ToList())
            {
                _players[boundPlayerId].Pokemon[0].Info.State.Bind = 0;
                _binds.Remove(boundPlayerId);
            }
        }
    }
}
