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

        if (DefaultTurnInfo(battle.TurnInfo))
            return;

        BattleTurnInfo(battle.Info, battle.SkillId);
    }

	static bool DefaultTurnInfo(RepeatedField<BattleInfo> battleInfoList)
	{
		foreach (BattleInfo battleInfo in battleInfoList)
        {
            string announce = "";
            string toName = battleInfo.ToData.Name;
            float targetHPRatio = Util.GetPokemonHPRatio(battleInfo.ToData);
            TargetType targetType = TargetType.End;

            if (battleInfo.TargetType == TargetType.Oneself)
                targetType = Managers.Player.MyTurn ? TargetType.Oneself : TargetType.Enemy;
            else
                targetType = Managers.Player.MyTurn ? TargetType.Enemy : TargetType.Oneself;
             
            if (battleInfo.ApplyType == ApplyType.Dot)
            {
                string skillName = Managers.Data.SkillDict[battleInfo.LatingSkillId].name;
                announce = $"{toName}은(는) {skillName}에 의해 지속데미지를 받고있다.";
            }
            else if (battleInfo.ApplyType == ApplyType.StatusEffect)
            {
                announce = $"{toName}은(는) 지속데미지를 받고있다.";
            }

			UIBattleScene battleScene = Managers.UI.SceneUI.GetComponent<UIBattleScene>();
            Managers.Job.Push(() => { battleScene.SetAnnounce(announce, true); });
            Managers.Job.Push(() => { battleScene.SetHPBar(targetHPRatio, targetType, true); });

            if (ChechPokemonFallDown(battleScene, toName, targetHPRatio, true))
                return true;
        }

        return false;
    }

	static void BattleTurnInfo(RepeatedField<BattleInfo> battleInfoList, int skillId)
    {
        UIBattleScene battleScene = Managers.UI.SceneUI.GetComponent<UIBattleScene>();
        string skillName = Managers.Data.SkillDict[skillId].name;

        {
            string fromName = battleInfoList[0].FromData.Name;
            if (Managers.Player.MyTurn == false)
                Managers.Job.Push(() => { battleScene.SetAnnounce($"{fromName}의 {skillName}!", true); });
        }

        foreach (BattleInfo battleInfo in battleInfoList)
        {
            TargetType targetType = TargetType.End;
            string fromName = battleInfo.FromData.Name;
            string toName = battleInfo.ToData.Name;
            float targetHPRatio = Util.GetPokemonHPRatio(battleInfo.ToData);

            if (battleInfo.TargetType == TargetType.Oneself)
                targetType = Managers.Player.MyTurn ? TargetType.Oneself : TargetType.Enemy;
            else
                targetType = Managers.Player.MyTurn ? TargetType.Enemy : TargetType.Oneself;


            if ((battleInfo.StateFlag & BattleStateFlag.Miss) == BattleStateFlag.Miss)
            {
                string announce = $"{fromName}의 공격은 빗나갔다!";
                Managers.Job.Push(() => { battleScene.SetAnnounce(announce, true); });
                return;
            }

            if (battleInfo.ApplyType == ApplyType.Atk || battleInfo.ApplyType == ApplyType.Spa || battleInfo.ApplyType == ApplyType.Dot)
            {
                if (battleInfo.TargetType == TargetType.Oneself)
                {
                    string announce = $"{toName}은(는) 반동으로 인해 데미지를 입었다.";
                    Managers.Job.Push(() => { battleScene.SetHPBar(targetHPRatio, targetType, true); });
                    Managers.Job.Push(() => { battleScene.SetAnnounce(announce, true); });

                    ChechPokemonFallDown(battleScene, toName, targetHPRatio, true);
                }
                else
                {
                    Managers.Job.Push(() => { battleScene.SetHPBar(targetHPRatio, targetType, true); });

                    if ((battleInfo.StateFlag & BattleStateFlag.Critical) == BattleStateFlag.Critical)
                        Managers.Job.Push(() => { battleScene.SetAnnounce("급소에 맞았다!", true); });

                    bool isEffective = (battleInfo.StateFlag & BattleStateFlag.Effective) == BattleStateFlag.Effective;
                    bool isIneffective = (battleInfo.StateFlag & BattleStateFlag.Ineffective) == BattleStateFlag.Ineffective;
                    if (isEffective || isIneffective)
                        Managers.Job.Push(() => { battleScene.SetAnnounce(isEffective ? "효과는 굉장했다." : "효과가 별로인듯 하다.", true); });

                    ChechPokemonFallDown(battleScene, toName, targetHPRatio, false);
                }
            }
            else
            {
                string announce = GetNoneAttackBattleAnnounce(battleInfo);
                Managers.Job.Push(() => { battleScene.SetAnnounce(announce, true); });
            }
        }
    }

    static bool ChechPokemonFallDown(UIBattleScene battleScene, string fallDownPokemonName, float hpRatio, bool isMyPokemon)
    {
        if (hpRatio > 0f)
            return false;

        Managers.Job.Push(() => { battleScene.SetAnnounce($"{fallDownPokemonName}은(는) 쓰려졌다.", true); });

        bool isShowChangePokemonPopup = (Managers.Player.MyTurn && isMyPokemon) || (Managers.Player.MyTurn == false && isMyPokemon == false);
        if (isShowChangePokemonPopup)
            Managers.Job.Push(() => { battleScene.ChangeFalldownPokemon(); });

        return true;
    }

    static string GetNoneAttackBattleAnnounce(BattleInfo battleInfo)
    {
        string valueTypeWord = string.Empty;
        string valueWord = battleInfo.SkillValue == 1 ? string.Empty : "크게 ";
        string behaviorWord = string.Empty;

        switch (battleInfo.ApplyType)
        {
            case ApplyType.Recovery:
                valueTypeWord = "체력을";
                behaviorWord = "회복했다";
                break;
            case ApplyType.BuffAtk:
                valueTypeWord = "공격력이";
                behaviorWord = "올라갔다";
                break;
            case ApplyType.BuffSpa:
                valueTypeWord = "특수공격력이";
                behaviorWord = "올라갔다";
                break;
            case ApplyType.BuffDef:
                valueTypeWord = "방어력이";
                behaviorWord = "올라갔다";
                break;
            case ApplyType.BuffSpd:
                valueTypeWord = "특수방어력이";
                behaviorWord = "올라갔다";
                break;
            case ApplyType.BuffSpe:
                valueTypeWord = "스피드가";
                behaviorWord = "올라갔다";
                break;
            case ApplyType.StatusEffect:
                behaviorWord = "상태이상에 걸렸다";
                break;
            case ApplyType.DebuffAtk:
                valueTypeWord = "공격력이";
                behaviorWord = "내려갔다";
                break;
            case ApplyType.DebuffSpa:
                valueTypeWord = "특수공격력이";
                behaviorWord = "내려갔다";
                break;
            case ApplyType.DebuffDef:
                valueTypeWord = "방어력이";
                behaviorWord = "내려갔다";
                break;
            case ApplyType.DebuffSpd:
                valueTypeWord = "특수방어력이";
                behaviorWord = "내려갔다";
                break;
            case ApplyType.DebuffSpe:
                valueTypeWord = "스피드가";
                behaviorWord = "내려갔다";
                break;
            case ApplyType.Sturn:
                behaviorWord = "기절했다";
                break;
            case ApplyType.Confusion:
                behaviorWord = "혼란에 빠졌다";
                break;
        }

        return $"{battleInfo.ToData.Name}의 {valueTypeWord} {valueWord}{behaviorWord}.";
    }

    public static void S_TurnPokeballHandler(PacketSession session, IMessage packet)
    {
        S_TurnPokeball pokeball = (S_TurnPokeball)packet;
    }

    public static void S_TurnChangeHandler(PacketSession session, IMessage packet)
    {
        S_TurnChange change = (S_TurnChange)packet;
    }

    public static void S_TurnRunawayHandler(PacketSession session, IMessage packet)
    {
        S_TurnRunaway runaway = (S_TurnRunaway)packet;
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
		battleScene.MyTurn();
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
        battleScene.SetBattlePokemonInfo();
    }

    public static void S_DualEndHandler(PacketSession session, IMessage packet)
    {
        S_DualEnd dualEndPacket = new S_DualEnd();
        foreach (DualResultInfo dualResultInfo in dualEndPacket.DualResultInfo)
        {
            if (dualResultInfo.PlayerId != Managers.Player.MyPlayer.Id)
                continue;

            UIBattleScene battleScene = Managers.UI.SceneUI.GetComponent<UIBattleScene>();
            if (battleScene == null)
            {
                Debug.Assert(false, "Cannot Found BattleScene!!");
            }

            battleScene.DualEnd(dualResultInfo.IsWin);
        }
    }
}