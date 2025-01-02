using Data;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.Protocol;
using ServerCore;
using System;
using System.Drawing;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

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

        bool isTurnOver = DefaultTurnInfo(battle.TurnInfo);
        if (isTurnOver)
            return;

        BattleTurnInfo(battle.Info, battle.SkillId);
    }

	static bool DefaultTurnInfo(RepeatedField<BattleInfo> turnInfoList)
    {
        UIBattleScene battleScene = Managers.UI.SceneUI.GetComponent<UIBattleScene>();
        if (battleScene == null)
        {
            Debug.Assert(false, "Fail to Found BattleScene!!");
        }

        bool isDuelEnd = false;
        if (turnInfoList.Count != 0)
            isDuelEnd = (turnInfoList[turnInfoList.Count - 1].StateFlag & BattleStateFlag.DuelEnd) == BattleStateFlag.DuelEnd;

        bool isTurnOver = false;
		foreach (BattleInfo turnInfo in turnInfoList)
        {
            if((turnInfo.StateFlag & BattleStateFlag.DuelEnd) == BattleStateFlag.DuelEnd)
                continue;

            string toName = turnInfo.ToData.Name;
            float targetHPRatio = Util.GetPokemonHPRatio(turnInfo.ToData);
            TargetType targetType = TargetType.End;

            if (turnInfo.TargetType == TargetType.Oneself)
                targetType = Managers.Player.MyTurn ? TargetType.Oneself : TargetType.Enemy;
            else
                targetType = Managers.Player.MyTurn ? TargetType.Enemy : TargetType.Oneself;

            if ((turnInfo.StateFlag & BattleStateFlag.DebuffDot) == BattleStateFlag.DebuffDot)
            {
                string skillName = Managers.Data.SkillDict[turnInfo.LatingSkillId].name;
                string announce = $"{toName}은(는) {skillName}에 의해 지속데미지를 받고있다.";
                Managers.Job.Push(() => { battleScene.SetAnnounce(announce, true); });
                Managers.Job.Push(() => { battleScene.SetHPBar(targetHPRatio, targetType, true); });
            }

            if ((turnInfo.StateFlag & BattleStateFlag.DebuffFire) == BattleStateFlag.DebuffPoison)
            {
                string announce = $"{toName}은(는) 지속데미지를 받고있다.";
                Managers.Job.Push(() => { battleScene.SetAnnounce(announce, true); });
                Managers.Job.Push(() => { battleScene.SetHPBar(targetHPRatio, targetType, true); });
            }

            if ((turnInfo.StateFlag & BattleStateFlag.DebuffConfusion) == BattleStateFlag.DebuffConfusion)
            {
                string announce = $"{toName}(은)는 혼란에 빠져 있다!";
                Managers.Job.Push(() => { battleScene.SetAnnounce(announce, true); });
                if (turnInfo.LatingSkillId == confusionSkillId)
                {
                    string text = $"{toName}(은)는 영문도 모른 채 자신을 공격했다!";
                    float hpRatio = Util.GetPokemonHPRatio(turnInfo.ToData);
                    Managers.Job.Push(() => { battleScene.SetAnnounce(text, true); });
                    Managers.Job.Push(() => { battleScene.SetHPBar(targetHPRatio, targetType, true); });
                    isTurnOver = true;
                }
            }
            else if ((turnInfo.StateFlag & BattleStateFlag.RecoveryConfusion) == BattleStateFlag.RecoveryConfusion)
            {
                string announce = $"{toName}(은)는 혼란에서 풀려났다!";
                Managers.Job.Push(() => { battleScene.SetAnnounce(announce, true); });
            }

            if ((turnInfo.StateFlag & BattleStateFlag.DebuffSturn) == BattleStateFlag.DebuffSturn)
            {
                string text = $"{toName}(은)는 풀이 죽어 기술을 쓸 수 없다!";
                Managers.Job.Push(() => { battleScene.SetAnnounce(text, true); });
                isTurnOver = true;
            }
            else if ((turnInfo.StateFlag & BattleStateFlag.RecoverySturn) == BattleStateFlag.RecoverySturn)
            {
                string announce = $"{toName}(은)는 풀 죽음 상태에서 회복했다!";
                Managers.Job.Push(() => { battleScene.SetAnnounce(announce, true); });
            }

            if (ChechPokemonFallDown(battleScene, toName, targetHPRatio, true, isDuelEnd))
                return true;
        }

        return isTurnOver;
    }

	static void BattleTurnInfo(RepeatedField<BattleInfo> battleInfoList, int skillId)
    {
        if (battleInfoList == null)
            return;

        UIBattleScene battleScene = Managers.UI.SceneUI.GetComponent<UIBattleScene>();
        string skillName = Managers.Data.SkillDict[skillId].name;

        {
            string fromName = battleInfoList[0].FromData.Name;
            Managers.Job.Push(() => { battleScene.SetAnnounce($"{fromName}의 {skillName}!", true); });
        }

        bool isDuelEnd = false;
        if(battleInfoList.Count > 0)
            isDuelEnd = (battleInfoList[battleInfoList.Count - 1].StateFlag & BattleStateFlag.DuelEnd) == BattleStateFlag.DuelEnd;

        foreach (BattleInfo battleInfo in battleInfoList)
        {
            if ((battleInfo.StateFlag & BattleStateFlag.DuelEnd) == BattleStateFlag.DuelEnd)
                continue;

            TargetType targetType = TargetType.End;
            float targetHPRatio = Util.GetPokemonHPRatio(battleInfo.ToData);

            if (battleInfo.TargetType == TargetType.Oneself)
                targetType = Managers.Player.MyTurn ? TargetType.Oneself : TargetType.Enemy;
            else
                targetType = Managers.Player.MyTurn ? TargetType.Enemy : TargetType.Oneself;

            if ((battleInfo.StateFlag & BattleStateFlag.Miss) == BattleStateFlag.Miss)
            {
                string fromName = battleInfo.FromData.Name;
                string announce = $"{fromName}의 공격은 빗나갔다!";
                Managers.Job.Push(() => { battleScene.SetAnnounce(announce, true); });
                return;
            }

            if (battleInfo.ApplyType == ApplyType.Atk || battleInfo.ApplyType == ApplyType.Spa || battleInfo.ApplyType == ApplyType.Dot)
            {
                string toName = battleInfo.ToData.Name;

                if (battleInfo.TargetType == TargetType.Oneself)
                {
                    string announce = $"{toName}은(는) 반동으로 인해 데미지를 입었다.";
                    Managers.Job.Push(() => { battleScene.SetHPBar(targetHPRatio, targetType, true); });
                    Managers.Job.Push(() => { battleScene.SetAnnounce(announce, true); });

                    ChechPokemonFallDown(battleScene, toName, targetHPRatio, true, isDuelEnd);
                }
                else
                {
                    Managers.Job.Push(() => { battleScene.SetHPBar(targetHPRatio, targetType, true); });

                    if ((battleInfo.StateFlag & BattleStateFlag.Critical) == BattleStateFlag.Critical)
                        Managers.Job.Push(() => { battleScene.SetAnnounce("급소에 맞았다!", true); });

                    bool isEffective = (battleInfo.StateFlag & BattleStateFlag.Effective) == BattleStateFlag.Effective;
                    bool isIneffective = (battleInfo.StateFlag & BattleStateFlag.Ineffective) == BattleStateFlag.Ineffective;
                    bool isNoneEffective = (battleInfo.StateFlag & BattleStateFlag.Noneeffective) == BattleStateFlag.Noneeffective;
                    if (isEffective || isIneffective || isNoneEffective)
                        Managers.Job.Push(() => { battleScene.SetAnnounce(isNoneEffective ? ("효과가 없는 것 같다...") : (isEffective ? "효과는 굉장했다." : "효과가 별로인듯 하다."), true); });

                    ChechPokemonFallDown(battleScene, toName, targetHPRatio, false, isDuelEnd);
                }
            }
            else
            {
                string announce = GetNoneAttackBattleAnnounce(battleInfo);
                Managers.Job.Push(() => { battleScene.SetAnnounce(announce, true); });
                Managers.Job.Push(() => { battleScene.SetHPBar(targetHPRatio, targetType, true); });
            }

            if (targetType == TargetType.Oneself)
                Managers.Player.MyPlayer.SetCurPokemonData(battleInfo.ToData);
            else
                Managers.Player.Enemy.SetCurPokemonData(battleInfo.ToData);
        }
    }

    static bool ChechPokemonFallDown(UIBattleScene battleScene, string fallDownPokemonName, float hpRatio, bool isMyPokemon, bool isDuelEnd)
    {
        if (hpRatio > 0f)
            return false;

        Managers.Job.Push(() => { battleScene.SetAnnounce($"{fallDownPokemonName}은(는) 쓰려졌다.", true); });

        if (isDuelEnd == false)
        {
            // TODO POkEMON IDLE 애니메이션 재생

            bool isShowChangePokemonPopup = (Managers.Player.MyTurn && isMyPokemon) || (Managers.Player.MyTurn == false && isMyPokemon == false);
            if (isShowChangePokemonPopup)
                Managers.Job.Push(() => { battleScene.ChangeFalldownPokemon(); });
        }

        return true;
    }

    static string GetNoneAttackBattleAnnounce(BattleInfo battleInfo)
    {
        string valueTypeWord = string.Empty;
        string valueWord = battleInfo.SkillValue == 1 ? string.Empty : " 크게 ";
        string behaviorWord = string.Empty;

        switch (battleInfo.ApplyType)
        {
            case ApplyType.Recovery:
                valueTypeWord = "(은)는 체력을 ";
                behaviorWord = "회복했다";
                break;
            case ApplyType.BuffAtk:
                valueTypeWord = "의 공격력이 ";
                behaviorWord = "올라갔다";
                break;
            case ApplyType.BuffSpa:
                valueTypeWord = "의 특수공격력이 ";
                behaviorWord = "올라갔다";
                break;
            case ApplyType.BuffDef:
                valueTypeWord = "의 방어력이 ";
                behaviorWord = "올라갔다";
                break;
            case ApplyType.BuffSpd:
                valueTypeWord = "의 특수방어력이 ";
                behaviorWord = "올라갔다";
                break;
            case ApplyType.BuffSpe:
                valueTypeWord = "의 스피드가 ";
                behaviorWord = "올라갔다";
                break;
            case ApplyType.StatusEffect:
                behaviorWord = "(은)는 상태이상에 걸렸다";
                break;
            case ApplyType.DebuffAtk:
                valueTypeWord = "의 공격력이 ";
                behaviorWord = "내려갔다";
                break;
            case ApplyType.DebuffSpa:
                valueTypeWord = "의 특수공격력이 ";
                behaviorWord = "내려갔다";
                break;
            case ApplyType.DebuffDef:
                valueTypeWord = "의 방어력이 ";
                behaviorWord = "내려갔다";
                break;
            case ApplyType.DebuffSpd:
                valueTypeWord = "의 특수방어력이 ";
                behaviorWord = "내려갔다";
                break;
            case ApplyType.DebuffSpe:
                valueTypeWord = "의 스피드가 ";
                behaviorWord = "내려갔다";
                break;
            case ApplyType.Sturn:
                behaviorWord = "(은)는 풀이 죽었다";
                break;
            case ApplyType.Confusion:
                behaviorWord = "(은)는 혼란에 빠졌다";
                break;
        }

        string resultString = $"{battleInfo.ToData.Name}{valueTypeWord}{valueWord}{behaviorWord}.";
        return resultString;
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

    public static void S_TurnHandler(PacketSession session, IMessage packet)
    {
        S_Turn turn = (S_Turn)packet;
        Managers.Player.MyTurn = turn.MyTurn;

        UIBattleScene battleScene = Managers.UI.SceneUI.GetComponent<UIBattleScene>();
		battleScene.NewTurn();
    }

    public static void S_ChangePokemonHandler(PacketSession session, IMessage packet)
    {
        S_ChangePokemon changePokemonPacket = (S_ChangePokemon)packet;
         
        UIBattleScene battleScene = Managers.UI.SceneUI.GetComponent<UIBattleScene>();
        if (battleScene == null)
        {
            Debug.Assert(false, "Cannot Found BattleScene!!");
        }

        bool isMyPokemon = changePokemonPacket.PlayerId == Managers.Player.MyPlayer.Id;
        if (isMyPokemon)
            Managers.Player.MyPlayer.ChangePokemonOrderById(changePokemonPacket.ChangePokemonId);
        else
            Managers.Player.Enemy.ChangePokemonOrderById(changePokemonPacket.ChangePokemonId);

        battleScene.SetBattlePokemonInfo(isMyPokemon);
        Managers.Job.Excute();
    }

    public static void S_DuelEndHandler(PacketSession session, IMessage packet)
    {
        S_DuelEnd dualEndPacket = (S_DuelEnd)packet;

        UIBattleScene battleScene = Managers.UI.SceneUI.GetComponent<UIBattleScene>();
        if (battleScene == null)
        {
            Debug.Assert(false, "Cannot Found BattleScene!!");
        }

        battleScene.DuelEnd(dualEndPacket.IsWin, dualEndPacket.IsRunaway);
    }
}