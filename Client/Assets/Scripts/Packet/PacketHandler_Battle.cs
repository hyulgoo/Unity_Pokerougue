using Data;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.Protocol;
using ServerCore;
using System;
using System.Collections.Generic;
using UnityEngine;

// 전투는 라운드 단위다. 두 사람이 행동을 고르면 서버가 순서대로 처리한 결과를 보낸다.
//  S_TurnBattle(PlayerId = 행동한 사람) : 기술 전 상태(TurnInfo)와 기술 결과(Info)
//  S_TurnBattle(PlayerId = 0)          : 라운드가 끝날 때의 데미지(TurnInfo)
//  S_ChangePokemon                     : 교체
// 각 BattleInfo의 ToId/FromId는 그 포켓몬의 주인이라, 누구 쪽에 그릴지는 이것으로 정한다.
partial class PacketHandler
{
    const int confusionSkillId = 999;

    public static void S_StartBattleHandler(PacketSession session, IMessage packet)
    {
        S_StartBattle startbattle = (S_StartBattle)packet;

		Managers.Scene.LoadScene(Define.Scene.Battle);

        Managers.Player.Add(startbattle.MyInfo, myPlayer: true);
		Managers.Player.Add(startbattle.EnemyInfo, myPlayer: false);

		Managers.Player.MyPlayer.SetPokemonData(startbattle.FromPokemon);
        Managers.Player.Enemy.SetPokemonData(startbattle.ToPokemon);
		Managers.Player.ArenaType = startbattle.ArenaType;
		Managers.Player.MyTurn = startbattle.IsMyTurn;
    }

    public static void S_TurnBattleHandler(PacketSession session, IMessage packet)
    {
        S_TurnBattle battle = (S_TurnBattle)packet;
		Managers.Player.IsTurnProgressing = true;

        bool isBlocked = DefaultTurnInfo(battle.TurnInfo);
        if (isBlocked || battle.Info.Count == 0 || battle.Info[0].FromData == null)
            return;

        bool isActorMe = IsMine(battle.PlayerId);

        // 기술이 실제로 나갔을 때만 PP가 준다.
        if (isActorMe)
        {
            int pokemonId = battle.Info[0].FromData.Id;
            Dictionary<int, Dictionary<int, int>> skillPP = Managers.Player.MyPlayer.SkillPP;
            if (skillPP.TryGetValue(pokemonId, out Dictionary<int, int> pp) && pp.ContainsKey(battle.SkillId))
                pp[battle.SkillId] = Math.Max(0, pp[battle.SkillId] - 1);
        }

        BattleTurnInfo(battle.Info, battle.SkillId, isActorMe);
    }

    static bool IsMine(int playerId)
    {
        return playerId == Managers.Player.MyPlayer.Id;
    }

    static TargetType SideOf(int playerId)
    {
        return IsMine(playerId) ? TargetType.Oneself : TargetType.Enemy;
    }

    static bool Has(BattleStateFlag flags, BattleStateFlag flag)
    {
        return (flags & flag) == flag;
    }

    static string SkillName(int skillId)
    {
        return Managers.Data.SkillDict.TryGetValue(skillId, out SkillData skill) ? skill.name : string.Empty;
    }

    // 받은 결과로 그 포켓몬의 데이터를 바꾼다. 같은 라운드에 교체 연출이 남아 있어도 엉뚱한 포켓몬을 덮어쓰지 않게 ID로 찾는다.
    static void StoreData(int playerId, PokemonData data)
    {
        if (data == null)
            return;

        CommonPlayerController owner = IsMine(playerId) ? Managers.Player.MyPlayer : Managers.Player.Enemy;
        owner.SetPokemonDataById(data);
    }

    static void Announce(UIBattleScene battleScene, string text)
    {
        Managers.Job.Push(() => { battleScene.SetAnnounce(text, true); });
    }

    static void HPBar(UIBattleScene battleScene, float ratio, TargetType side)
    {
        Managers.Job.Push(() => { battleScene.SetPokemonHPBar(ratio, side, true); });
    }

    // 같은 쪽이 두 번 들어오면 마지막 HP로 한 번만 쓰러뜨린다.
    static void AddFaint(List<(string name, float hpRatio, bool isMe)> faints, string name, float hpRatio, bool isMe)
    {
        faints.RemoveAll(faint => faint.isMe == isMe);
        faints.Add((name, hpRatio, isMe));
    }

    // 기술 전 상태(풀죽음·혼란·마비)와 라운드가 끝날 때의 데미지(화상·씨뿌리기·묶기)·순풍 종료. 기술이 막혔으면 true.
	static bool DefaultTurnInfo(RepeatedField<BattleInfo> turnInfoList)
    {
        UIBattleScene battleScene = Managers.UI.SceneUI.GetComponent<UIBattleScene>();
        if (battleScene == null)
        {
            Debug.Assert(false, "Fail to Found BattleScene!!");
        }

        bool isDuelEnd = false;
        if (turnInfoList.Count != 0)
            isDuelEnd = Has(turnInfoList[turnInfoList.Count - 1].StateFlag, BattleStateFlag.DuelEnd);

        bool isBlocked = false;
        List<(string name, float hpRatio, bool isMe)> faints = new List<(string, float, bool)>();

		foreach (BattleInfo turnInfo in turnInfoList)
        {
            BattleStateFlag flag = turnInfo.StateFlag;
            if (Has(flag, BattleStateFlag.DuelEnd) || turnInfo.ToData == null)
                continue;

            string toName = turnInfo.ToData.Name;
            float targetHPRatio = Util.GetPokemonHPRatio(turnInfo.ToData);
            TargetType targetType = SideOf(turnInfo.ToId);
            bool isMe = IsMine(turnInfo.ToId);
            StoreData(turnInfo.ToId, turnInfo.ToData);

            if (Has(flag, BattleStateFlag.DebuffSturn))
            {
                Announce(battleScene, $"{toName}(은)는 풀이 죽어 움직일 수 없었다!");
                isBlocked = true;
            }

            if (Has(flag, BattleStateFlag.DebuffConfusion))
            {
                Announce(battleScene, $"{toName}(은)는 혼란에 빠져 있다!");
                if (turnInfo.LatingSkillId == confusionSkillId)
                {
                    Announce(battleScene, "영문도 모른 채 자신을 공격했다!");
                    HPBar(battleScene, targetHPRatio, targetType);
                    isBlocked = true;
                }
            }
            else if (Has(flag, BattleStateFlag.RecoveryConfusion))
            {
                Announce(battleScene, $"{toName}의 혼란이 풀렸다!");
            }

            if (Has(flag, BattleStateFlag.DebuffParalysis))
            {
                Announce(battleScene, $"{toName}(은)는 몸이 저려서 움직일 수 없다!");
                isBlocked = true;
            }

            if (Has(flag, BattleStateFlag.DebuffFire))
            {
                Announce(battleScene, $"{toName}(은)는 화상 데미지를 입었다!");
                HPBar(battleScene, targetHPRatio, targetType);
            }

            if (Has(flag, BattleStateFlag.DebuffDot))
            {
                Announce(battleScene, $"{SkillName(turnInfo.LatingSkillId)}(이)가 {toName}의 체력을 빼앗는다!");
                HPBar(battleScene, targetHPRatio, targetType);

                // 빼앗은 체력만큼 상대 자리의 포켓몬이 회복한다(FromData가 회복 뒤 상태다).
                if (turnInfo.FromData != null && turnInfo.FromData.Info.Hp > 0)
                {
                    StoreData(turnInfo.FromId, turnInfo.FromData);
                    HPBar(battleScene, Util.GetPokemonHPRatio(turnInfo.FromData), SideOf(turnInfo.FromId));
                }
            }

            if (Has(flag, BattleStateFlag.DebuffBind))
            {
                string bindName = SkillName(turnInfo.LatingSkillId);
                Announce(battleScene, $"{toName}(은)는 {bindName}의 데미지를 입고 있다!");
                HPBar(battleScene, targetHPRatio, targetType);
                if (Has(flag, BattleStateFlag.RecoveryBind))
                    Announce(battleScene, $"{toName}(은)는 {bindName}에서 벗어났다!");
            }

            if (Has(flag, BattleStateFlag.TailwindEnd))
                Announce(battleScene, $"{(isMe ? "우리 편" : "상대")}의 순풍이 멈췄다!");

            if (targetHPRatio <= 0f)
                AddFaint(faints, toName, targetHPRatio, isMe);
        }

        foreach ((string name, float hpRatio, bool isMe) in faints)
            ChechPokemonFallDown(battleScene, name, hpRatio, isMe, isDuelEnd);

        return isBlocked;
    }

	static void BattleTurnInfo(RepeatedField<BattleInfo> battleInfoList, int skillId, bool isAttackerMe)
    {
        UIBattleScene battleScene = Managers.UI.SceneUI.GetComponent<UIBattleScene>();
        string skillName = SkillName(skillId);

        Announce(battleScene, $"{battleInfoList[0].FromData.Name}의 {skillName}!");

        bool isDuelEnd = Has(battleInfoList[battleInfoList.Count - 1].StateFlag, BattleStateFlag.DuelEnd);
        bool isSkillAnimPlayed = false;

        // 쓰러짐은 기술의 효과(능력치 변화·반동)를 모두 보여 준 뒤에 처리한다.
        // 쓰러진 쪽의 교체를 기다리는 동안 남은 문구가 다른 문구와 겹치지 않게 한다.
        List<(string name, float hpRatio, bool isMe)> faints = new List<(string, float, bool)>();

        foreach (BattleInfo battleInfo in battleInfoList)
        {
            if (Has(battleInfo.StateFlag, BattleStateFlag.DuelEnd) || battleInfo.ToData == null)
                continue;

            TargetType targetType = SideOf(battleInfo.ToId);
            bool isTargetMe = IsMine(battleInfo.ToId);
            string toName = battleInfo.ToData.Name;
            float targetHPRatio = Util.GetPokemonHPRatio(battleInfo.ToData);
            StoreData(battleInfo.ToId, battleInfo.ToData);

            if (Has(battleInfo.StateFlag, BattleStateFlag.Miss))
            {
                Announce(battleScene, $"{battleInfo.FromData.Name}의 공격은 빗나갔다!");
                continue;
            }

            switch (battleInfo.ApplyType)
            {
                case ApplyType.Atk:
                case ApplyType.Spa:
                case ApplyType.Dot:
                    if (battleInfo.TargetType == TargetType.Oneself)
                    {
                        HPBar(battleScene, targetHPRatio, targetType);
                        Announce(battleScene, $"{toName}은(는) 반동으로 인해 데미지를 입었다.");
                    }
                    else
                    {
                        bool isEffective = Has(battleInfo.StateFlag, BattleStateFlag.Effective);
                        bool isIneffective = Has(battleInfo.StateFlag, BattleStateFlag.Ineffective);
                        bool isNoneEffective = Has(battleInfo.StateFlag, BattleStateFlag.Noneeffective);

                        // 두번치기처럼 피해 항목이 여러 개면 맞을 때마다 연출을 다시 재생한다.
                        string hitSoundPath = isNoneEffective ? null : (isEffective ? "effect/hit_strong" : (isIneffective ? "effect/hit_weak" : "effect/hit"));
                        Managers.Job.Push(() => { battleScene.PlaySkillAnim(skillId, isAttackerMe, false, hitSoundPath); });
                        isSkillAnimPlayed = true;

                        HPBar(battleScene, targetHPRatio, targetType);

                        if (Has(battleInfo.StateFlag, BattleStateFlag.Critical))
                            Announce(battleScene, "급소에 맞았다!");

                        if (isEffective || isIneffective || isNoneEffective)
                            Announce(battleScene, isNoneEffective ? "효과가 없는 것 같다..." : (isEffective ? "효과는 굉장했다." : "효과가 별로인듯 하다."));
                    }
                    break;

                case ApplyType.FaintSelf:
                    // 대폭발: 기술을 쓴 포켓몬도 쓰러진다.
                    HPBar(battleScene, 0f, targetType);
                    break;

                default:
                {
                    // 변화 기술의 연출. 공격에 딸린 효과(기가드레인의 회복 등)는 이미 재생했으니 다시 틀지 않는다.
                    if (isSkillAnimPlayed == false)
                    {
                        bool isSelfTarget = battleInfo.TargetType == TargetType.Oneself;
                        Managers.Job.Push(() => { battleScene.PlaySkillAnim(skillId, isAttackerMe, isSelfTarget, null); });
                        isSkillAnimPlayed = true;
                    }

                    string announce = GetNoneAttackBattleAnnounce(battleInfo, isTargetMe);
                    string sound = StatSound(battleInfo.ApplyType);
                    Announce(battleScene, announce);
                    Managers.Job.Push(() =>
                    {
                        battleScene.SetPokemonHPBar(targetHPRatio, targetType, true);
                        if (sound != null)
                            Managers.Sound.Play(sound);
                    });
                    break;
                }
            }

            if (targetHPRatio <= 0f)
                AddFaint(faints, toName, targetHPRatio, isTargetMe);
        }

        // 맞은 쪽이 먼저, 반동·대폭발로 쓰러진 공격자가 나중이다.
        faints.Sort((a, b) => (a.isMe == isAttackerMe).CompareTo(b.isMe == isAttackerMe));
        foreach ((string name, float hpRatio, bool isMe) in faints)
            ChechPokemonFallDown(battleScene, name, hpRatio, isMe, isDuelEnd);
    }

    // 능력치가 오르면 stat_up, 내리면 stat_down. 상태이상·회복은 소리가 없다.
    static string StatSound(ApplyType applyType)
    {
        if (applyType >= ApplyType.BuffAtk && applyType <= ApplyType.BuffSpe)
            return "effect/stat_up";
        if (applyType >= ApplyType.DebuffAtk && applyType <= ApplyType.DebuffSpe)
            return "effect/stat_down";
        return null;
    }

    static bool ChechPokemonFallDown(UIBattleScene battleScene, string fallDownPokemonName, float hpRatio, bool isMyPokemon, bool isDuelEnd)
    {
        if (hpRatio > 0f)
            return false;

        Managers.Job.Push(() => { battleScene.SetAnnounce($"{fallDownPokemonName}은(는) 쓰려졌다.", true); });

        Managers.Job.Push(() => { battleScene.ChangeFalldownPokemon(isMyPokemon, isDuelEnd); });

        return true;
    }

    static string GetNoneAttackBattleAnnounce(BattleInfo battleInfo, bool isTargetMe)
    {
        string name = battleInfo.ToData.Name;
        string rank = battleInfo.SkillValue == 1 ? string.Empty : "크게 ";

        switch (battleInfo.ApplyType)
        {
            case ApplyType.Recovery:
                // 회복 효과는 기가드레인 같은 흡수뿐이다.
                return $"{name}(은)는 상대의 체력을 흡수했다.";
            case ApplyType.BuffAtk:
                return $"{name}의 공격력이 {rank}올라갔다.";
            case ApplyType.BuffSpa:
                return $"{name}의 특수공격력이 {rank}올라갔다.";
            case ApplyType.BuffDef:
                return $"{name}의 방어력이 {rank}올라갔다.";
            case ApplyType.BuffSpd:
                return $"{name}의 특수방어력이 {rank}올라갔다.";
            case ApplyType.BuffSpe:
                return $"{name}의 스피드가 {rank}올라갔다.";
            case ApplyType.DebuffAtk:
                return $"{name}의 공격력이 {rank}내려갔다.";
            case ApplyType.DebuffSpa:
                return $"{name}의 특수공격력이 {rank}내려갔다.";
            case ApplyType.DebuffDef:
                return $"{name}의 방어력이 {rank}내려갔다.";
            case ApplyType.DebuffSpd:
                return $"{name}의 특수방어력이 {rank}내려갔다.";
            case ApplyType.DebuffSpe:
                return $"{name}의 스피드가 {rank}내려갔다.";
            case ApplyType.Confusion:
                return $"{name}(은)는 혼란에 빠졌다.";
            case ApplyType.Burn:
                return $"{name}(은)는 화상을 입었다!";
            case ApplyType.Paralysis:
                return $"{name}(은)는 마비되어 기술이 나오기 어려워졌다!";
            case ApplyType.Bind:
                return $"{name}(은)는 {SkillName(battleInfo.LatingSkillId)}에 갇혔다!";
            case ApplyType.Tailwind:
                return $"{(isTargetMe ? "우리 편" : "상대")} 뒤에서 순풍이 불기 시작했다!";
            default:
                return $"{name}(은)는 상태이상에 걸렸다.";
        }
    }

    public static void S_TurnPokeballHandler(PacketSession session, IMessage packet)
    {
        S_TurnPokeball pokeball = (S_TurnPokeball)packet;
    }

    public static void S_TurnChangeHandler(PacketSession session, IMessage packet)
    {
        S_TurnChange change = (S_TurnChange)packet;
    }

    public static void S_TurnPassHandler(PacketSession session, IMessage packet)
    {
        S_TurnPass turn = (S_TurnPass)packet;
    }

    // 새 라운드의 행동 선택, 또는 쓰러진 포켓몬의 교체 요청
    public static void S_TurnHandler(PacketSession session, IMessage packet)
    {
        S_Turn turn = (S_Turn)packet;
        Managers.Player.MyTurn = turn.MyTurn;

        UIBattleScene battleScene = Managers.UI.SceneUI.GetComponent<UIBattleScene>();
		battleScene.NewTurn(turn.MustChange);
    }

    public static void S_ChangePokemonHandler(PacketSession session, IMessage packet)
    {
        S_ChangePokemon changePokemonPacket = (S_ChangePokemon)packet;
        Managers.Player.IsTurnProgressing = true;

        UIBattleScene battleScene = Managers.UI.SceneUI.GetComponent<UIBattleScene>();
        if (battleScene == null)
            Debug.Assert(false, "Cannot Found BattleScene!!");

        bool isMyPokemon = IsMine(changePokemonPacket.PlayerId);
        int changePokemonId = changePokemonPacket.ChangePokemonId;

        // 묶은 포켓몬이 들어가면 반대편의 묶임이 풀린다. 서버는 따로 알리지 않는다.
        CommonPlayerController opposite = isMyPokemon ? Managers.Player.Enemy : Managers.Player.MyPlayer;
        if (opposite.IsCurPokemonBound())
            opposite.SetPokemonDataById(ReleasedFromBind(opposite.GetCurPokemonData()));

        // 같은 라운드의 앞선 연출이 끝난 뒤에 바꿔 내보낸다.
        Managers.Job.Push(() =>
        {
            CommonPlayerController owner = isMyPokemon ? Managers.Player.MyPlayer : Managers.Player.Enemy;
            owner.ChangePokemonOrderById(changePokemonId);
            battleScene.ThrowPokeball(isMyPokemon);
        });
    }

    static PokemonData ReleasedFromBind(PokemonData data)
    {
        PokemonData released = data.Clone();
        released.Info.State.Bind = 0;
        return released;
    }

    public static void S_DuelEndHandler(PacketSession session, IMessage packet)
    {
        S_DuelEnd dualEndPacket = (S_DuelEnd)packet;

        UIBattleScene battleScene = Managers.UI.SceneUI.GetComponent<UIBattleScene>();
        if (battleScene == null)
            Debug.Assert(false, "Cannot Found BattleScene!!");

        battleScene.DuelEnd(dualEndPacket.IsWin, dualEndPacket.IsRunaway);
    }
}
