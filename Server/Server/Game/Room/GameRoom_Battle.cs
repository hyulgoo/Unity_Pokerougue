using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.Collections;
using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game.Room
{
    public partial class GameRoom : JobSerializer
    {
        private const int confusionSkillId = 999;

        // playerId는 세션에서 얻은 행위자다.
        public void Turn(int playerId, C_Turn packet)
        {
            // 전투 중이고, 자기 턴이고, 이번 턴의 행동을 아직 받지 않았을 때만 처리한다.
            if (!_battleStarted || isWaitingPlayerTurnEnd || playerId != CurrentTurnPlayerId)
                return;

            if (packet.TurnInfo == null || _players.Count != (int)TargetType.End)
                return;

            if (packet.TurnInfo.Action == ActionType.Fight && !CanUseSkill(playerId, packet.TurnInfo.SkillId))
                return;

            // 아래 판정 코드가 읽는 PlayerId를 세션의 플레이어로 고정한다.
            packet.PlayerId = playerId;

            isWaitingPlayerTurnEnd = true;

            switch (packet.TurnInfo.Action)
            {
                case ActionType.Pass:
                    Pass(packet);
                    break;
                case ActionType.Fight:
                    Fight(packet);
                    break;
                case ActionType.Pokeball:
                    PokeBall(packet);
                    break;
                case ActionType.Change:
                    ChangePokemon(packet.PlayerId, packet.TurnInfo.ChangePokemonId);
                    break;
                case ActionType.Runaway:
                    DuelEnd(packet.PlayerId, true);
                    break;
            }
        }

        // 스킬이 데이터에 있고, 지금 나와 있는 포켓몬이 가진 스킬이어야 한다.
        private bool CanUseSkill(int playerId, int skillId)
        {
            return DataManager.SkillDict.ContainsKey(skillId)
                   && _players.TryGetValue(playerId, out Player player)
                   && player.Pokemon.Count > 0
                   && player.Pokemon[0].Info.SkillId.Contains(skillId);
        }

        private bool DefaultTurn(C_Turn packet, ref RepeatedField<BattleInfo> battleInfoList)
        {
            Random random = new Random();
            Player player = PlayerManager.Instance.Find(RoomId, packet.PlayerId);
            Player enemy = PlayerManager.Instance.Find(RoomId, FindEnemyIdByMyId(packet.PlayerId));
            PokemonData myPokemonData = player.Pokemon[0];
            PokemonData enemyPokemonData = enemy.Pokemon[0];
            PokemonData standardInfo = DataManager.PokemonDict[player.Pokemon[0].Id];
            bool isSturnOrConfusionAttackOneself = false;

            if (myPokemonData.Info.State.Fire > 0)
            {
                BattleInfo result = new BattleInfo();
                result.ApplyType = ApplyType.StatusEffect;
                result.TargetType = TargetType.Oneself;
                myPokemonData.Info.Hp = myPokemonData.Info.Hp - standardInfo.Info.Hp / 20;
                myPokemonData.Info.Hp = myPokemonData.Info.Hp < 0 ? 0 : myPokemonData.Info.Hp;
                myPokemonData.Info.State.Fire--;
                result.FromData = myPokemonData.Clone();
                result.ToData = myPokemonData.Clone();
                result.StateFlag |= BattleStateFlag.DebuffFire;

                DefaultTurnIsRemainPokemonExist(result);

                battleInfoList.Add(result);
            }

            if (myPokemonData.Info.State.Dot > 0)
            {
                BattleInfo result = new BattleInfo();
                result.ApplyType = ApplyType.Dot;
                result.TargetType = TargetType.Oneself;
                result.LatingSkillId = packet.TurnInfo.SkillId;
                myPokemonData.Info.Hp = myPokemonData.Info.Hp - standardInfo.Info.Hp / 7;
                myPokemonData.Info.Hp = myPokemonData.Info.Hp < 0 ? 0 : myPokemonData.Info.Hp;
                myPokemonData.Info.State.Dot--;
                result.FromData = enemyPokemonData.Clone();
                result.ToData = myPokemonData.Clone();
                result.StateFlag |= BattleStateFlag.DebuffDot;

                DefaultTurnIsRemainPokemonExist(result);

                battleInfoList.Add(result);
            }

            if (myPokemonData.Info.State.Poison > 0)
            {
                BattleInfo result = new BattleInfo();
                result.ApplyType = ApplyType.StatusEffect;
                result.TargetType = TargetType.Oneself;
                myPokemonData.Info.Hp -= standardInfo.Info.Hp / 20;
                myPokemonData.Info.Hp = myPokemonData.Info.Hp < 0 ? 0 : myPokemonData.Info.Hp;
                myPokemonData.Info.State.Poison--;
                result.FromData = myPokemonData.Clone();
                result.ToData = myPokemonData.Clone();
                result.StateFlag |= BattleStateFlag.DebuffPoison;

                DefaultTurnIsRemainPokemonExist(result);

                battleInfoList.Add(result);
            }

            if (myPokemonData.Info.State.Confusion > 0)
            {
                BattleInfo result = new BattleInfo
                {
                    ApplyType = ApplyType.Confusion,
                    TargetType = TargetType.Oneself,
                    FromData = myPokemonData.Clone(),
                    ToData = myPokemonData.Clone()
                };

                bool recovery = random.Next(0, 100) > 60;
                if (!recovery)
                {
                    myPokemonData.Info.State.Confusion -= 1;
                    result.StateFlag |= BattleStateFlag.DebuffConfusion;
                    if (myPokemonData.Info.State.Confusion == 0)
                        result.StateFlag |= BattleStateFlag.RecoveryConfusion;

                    battleInfoList.Add(result);

                    if (random.Next(0, 100) < 40)
                    {
                        result.LatingSkillId = confusionSkillId;
                        SkillData skillData = DataManager.SkillDict[confusionSkillId];
                        foreach (SkillEffect skillEffect in skillData.info.SkillEffect)
                        {
                            BattleInfo confusionInfo = Util.CalcBattle(myPokemonData, myPokemonData, skillEffect);
                            battleInfoList.Add(confusionInfo);
                        }

                        isSturnOrConfusionAttackOneself = true;
                    }
                }
                else
                {
                    result.StateFlag |= BattleStateFlag.RecoveryConfusion;
                    myPokemonData.Info.State.Confusion = 0;
                    battleInfoList.Add(result);
                }
            }

            if (myPokemonData.Info.State.Sturn > 0)
            {
                BattleInfo result = new BattleInfo();
                result.ApplyType = ApplyType.Sturn;
                result.TargetType = TargetType.Oneself;
                result.FromData = myPokemonData.Clone();
                result.ToData = myPokemonData.Clone();

                bool recovery = random.Next(0, 100) > 66;
                myPokemonData.Info.State.Sturn = recovery ? 0 : myPokemonData.Info.State.Sturn - 1;

                if (!recovery)
                {
                    myPokemonData.Info.State.Sturn -= 1;
                    result.StateFlag |= BattleStateFlag.DebuffSturn;
                    if (myPokemonData.Info.State.Sturn == 0)
                        result.StateFlag |= BattleStateFlag.RecoverySturn;
                    isSturnOrConfusionAttackOneself = true;
                }
                else
                {
                    result.StateFlag |= BattleStateFlag.RecoverySturn;
                    myPokemonData.Info.State.Sturn = 0;
                }

                battleInfoList.Add(result);
            }

            return isSturnOrConfusionAttackOneself;
        }

        private void DefaultTurnIsRemainPokemonExist(BattleInfo battleInfo)
        {
            Dictionary<int, bool> playerDuelResultArray = new Dictionary<int, bool>();
            foreach (Player playerIter in _players.Values)
                playerDuelResultArray.Add(playerIter.Id, playerIter.IsRemainPokemonExist());

            if (!playerDuelResultArray.Values.Contains(false))
                return;

            isWaitingPlayerTurnEnd = false;

            battleInfo.StateFlag |= BattleStateFlag.DuelEnd;
            foreach (KeyValuePair<int, bool> playerInfo in playerDuelResultArray)
                if (playerInfo.Value)
                {
                    battleInfo.WinPlayerId = playerInfo.Key;
                    break;
                }
        }

        private void Pass(C_Turn packet)
        {
            S_TurnPass passPacket = new S_TurnPass();

            passPacket.PlayerId = packet.PlayerId;
            RepeatedField<BattleInfo> battleInfoList = new RepeatedField<BattleInfo>();
            DefaultTurn(packet.Clone(), ref battleInfoList);
            passPacket.TurnInfo.AddRange(battleInfoList);

            Broadcast(passPacket);
        }

        private void Fight(C_Turn packet)
        {
            S_TurnBattle battlePacket = new S_TurnBattle();

            int enemyId = FindEnemyIdByMyId(packet.PlayerId);
            Player player = PlayerManager.Instance.Find(RoomId, packet.PlayerId);
            Player enemy = PlayerManager.Instance.Find(RoomId, enemyId);

            PokemonData myPokemonData = player.Pokemon[0];
            PokemonData enemyPokemonData = enemy.Pokemon[0];

            RepeatedField<BattleInfo> battleTurnResult = new RepeatedField<BattleInfo>();
            RepeatedField<BattleInfo> battleFightResult = new RepeatedField<BattleInfo>();

            bool isSturn = DefaultTurn(packet, ref battleTurnResult);
            if (!isSturn)
            {
                SkillData skillData = DataManager.SkillDict[packet.TurnInfo.SkillId];
                foreach (SkillEffect skillEffect in skillData.info.SkillEffect)
                {
                    BattleInfo battleInfo = Util.CalcBattle(myPokemonData, enemyPokemonData, skillEffect);
                    battleFightResult.Add(battleInfo);
                }
            }

            // 모든 포켓몬이 쓰러졌는 지(전투가 끝나는 지) 확인
            Dictionary<int, bool> playerDuelResultArray = new Dictionary<int, bool>();
            foreach (Player playerIter in _players.Values)
                playerDuelResultArray.Add(playerIter.Id, playerIter.IsRemainPokemonExist());

            bool isDuelEnd = playerDuelResultArray.Values.Contains(false);
            int winPlayerId = -1;
            if (isDuelEnd)
            {
                BattleInfo battleInfo = new BattleInfo();
                battleInfo.StateFlag |= BattleStateFlag.DuelEnd;
                foreach (KeyValuePair<int, bool> playerInfo in playerDuelResultArray)
                {
                    if (!playerInfo.Value)
                        continue;

                    battleInfo.WinPlayerId = playerInfo.Key;
                    winPlayerId = playerInfo.Key;
                    break;
                }

                battleFightResult.Add(battleInfo);
            }

            battlePacket.PlayerId = packet.PlayerId;
            battlePacket.SkillId = packet.TurnInfo.SkillId;
            battlePacket.TurnInfo.AddRange(battleTurnResult);
            battlePacket.Info.AddRange(battleFightResult);

            Broadcast(battlePacket);

            if (isDuelEnd)
                DuelEnd(winPlayerId, false);
        }

        private void PokeBall(C_Turn packet)
        {
            S_TurnPokeball pokeballPacket = new S_TurnPokeball();
            pokeballPacket.BallType = packet.TurnInfo.BallType;
            Broadcast(pokeballPacket);
        }

        public void DuelEnd(int PlayerId, bool isRunaway)
        {
            // 끝난 전투에는 턴·교체 패킷을 더 받지 않는다.
            _battleStarted = false;

            foreach (KeyValuePair<int, Player> playerInfo in _players)
            {
                S_DuelEnd duelEndPacket = new S_DuelEnd();
                duelEndPacket.IsWin = playerInfo.Key == PlayerId ? false : true;
                duelEndPacket.IsRunaway = isRunaway;
                playerInfo.Value.Session.Send(duelEndPacket);
                playerInfo.Value.Session.HandleReEnterHandler(playerInfo.Value.Room.RoomId);
            }
        }

        public void TurnEnd(int playerId)
        {
            if (!isWaitingPlayerTurnEnd)
                return;

            // 이 방에 없는 ID가 준비 목록에 새 항목으로 들어가지 않게 한다.
            if (!_playerReady.ContainsKey(playerId))
                return;

            _playerReady[playerId] = true;

            int readyPlayerCount = 0;
            foreach (bool ready in _playerReady.Values)
                readyPlayerCount = ready ? ++readyPlayerCount : readyPlayerCount;

            if (readyPlayerCount != _playerReady.Count)
                return;

            CurrentTurnPlayerId = PlayerManager.Instance.GetCurrentTurnPlayerId(RoomId);

            List<int> playerKeyArray = _players.Keys.ToList<int>();
            foreach (int myId in playerKeyArray)
            {
                S_Turn turnPacket = new S_Turn
                {
                    MyTurn = myId == CurrentTurnPlayerId
                };

                _playerReady[myId] = false;
                _players[myId].Session.Send(turnPacket);
            }

            isWaitingPlayerTurnEnd = false;
        }

        public void ChangePokemon(int playerId, int changePokemonId)
        {
            if (!_battleStarted || !_players.TryGetValue(playerId, out Player player))
                return;

            S_ChangePokemon changePokemonPacket = new S_ChangePokemon
            {
                PlayerId = playerId,
                ChangePokemonId = changePokemonId
            };

            List<PokemonData> pokemonDataList = player.Pokemon;
            for (int index = 0; index < pokemonDataList.Count; ++index)
            {
                if (pokemonDataList[index].Id != changePokemonId)
                    continue;

                (pokemonDataList[0], pokemonDataList[index]) = (pokemonDataList[index], pokemonDataList[0]);
                Broadcast(changePokemonPacket);
                return;
            }

            // 자기 목록에 없는 포켓몬으로의 교체 요청은 무시한다.
        }
    }
}