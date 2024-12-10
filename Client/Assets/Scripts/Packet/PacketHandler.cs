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

class PacketHandler
{
	public static void S_EnterGameHandler(PacketSession session, IMessage packet)
	{
		S_EnterGame enterGamePacket = packet as S_EnterGame;
		Managers.Player.Add(enterGamePacket.Player, myPlayer: true);
		Managers.UI.SetLobbyPlayer();
	}

	public static void S_LeaveGameHandler(PacketSession session, IMessage packet)
	{
		S_LeaveGame leaveGameHandler = packet as S_LeaveGame;
		Managers.Player.Clear();
    }

	public static void S_SpawnHandler(PacketSession session, IMessage packet)
	{
		S_Spawn spawnPacket = packet as S_Spawn;
		foreach (ObjectInfo obj in spawnPacket.Objects)
		{
			Managers.Player.Add(obj, myPlayer: false);
        }

        Managers.UI.SetLobbyPlayer();
    }

	public static void S_DespawnHandler(PacketSession session, IMessage packet)
	{
		S_Despawn despawnPacket = packet as S_Despawn;
		foreach (int id in despawnPacket.ObjectIds)
		{
			Managers.Player.Remove(id);
        }
        Managers.UI.SetLobbyPlayer();
	}

	public static void S_ConnectedHandler(PacketSession session, IMessage packet)
	{
		C_Login loginPacket = new C_Login();

		string path = Managers.Network.Name;
		loginPacket.UniqueId = path.GetHashCode().ToString();
		Managers.Network.Send(loginPacket);
	}

	// 로그인 OK + 캐릭터 목록
	public static void S_LoginHandler(PacketSession session, IMessage packet)
	{
		S_Login loginPacket = (S_Login)packet;

		// TODO : 로비 UI에서 캐릭터 보여주고, 선택할 수 있도록
		if (loginPacket.Players == null || loginPacket.Players.Count == 0)
		{
			C_CreatePlayer createPacket = new C_CreatePlayer();
			createPacket.Name = Managers.Network.Name;
			Managers.Network.Send(createPacket);
		}
		else
		{
			for(int i = 0; i <  loginPacket.Players.Count; i++)
            {
                LobbyPlayerInfo info = loginPacket.Players[i];
                if (i == 0)
				{
                    C_EnterGame enterGamePacket = new C_EnterGame();
                    enterGamePacket.Name = info.Name;
                    Managers.Network.Send(enterGamePacket);
                }
				else
                {
					ObjectInfo playerinfo = new ObjectInfo();
					playerinfo.ObjectId = info.PlayerDbId;
					playerinfo.Name = info.Name;

					Managers.Player.Add(playerinfo, myPlayer: false);
				}
			}
        }
        Managers.UI.SetLobbyPlayer();
    }

	public static void S_CreatePlayerHandler(PacketSession session, IMessage packet)
    {
        S_CreatePlayer createOkPacket = (S_CreatePlayer)packet;

		if (createOkPacket.Player == null)
		{
			C_CreatePlayer createPacket = new C_CreatePlayer();
			createPacket.Name = Managers.Network.Name;
			Managers.Network.Send(createPacket);
		}
		else
		{
			C_EnterGame enterGamePacket = new C_EnterGame();
			enterGamePacket.Name = createOkPacket.Player.Name;
			Managers.Network.Send(enterGamePacket);
		}
	}

	public static void S_ItemListHandler(PacketSession session, IMessage packet)
    {
        S_ItemList itemList = (S_ItemList)packet;

		Managers.Inven.Clear();

		// 메모리에 아이템 정보 적용
		foreach (ItemInfo itemInfo in itemList.Items)
		{
			Item item = Item.MakeItem(itemInfo);
			Managers.Inven.Add(item);
		}
	}

	public static void S_AddItemHandler(PacketSession session, IMessage packet)
	{
		S_AddItem itemList = (S_AddItem)packet;

		// 메모리에 아이템 정보 적용
		foreach (ItemInfo itemInfo in itemList.Items)
		{
			Item item = Item.MakeItem(itemInfo);
			Managers.Inven.Add(item);
		}

		Debug.Log("아이템을 획득했습니다!");

		UI_GameScene gameSceneUI = Managers.UI.SceneUI as UI_GameScene;
		gameSceneUI.InvenUI.RefreshUI();
		gameSceneUI.StatUI.RefreshUI();
	}

	public static void S_EquipItemHandler(PacketSession session, IMessage packet)
	{
		S_EquipItem equipItemOk = (S_EquipItem)packet;

		// 메모리에 아이템 정보 적용
		Item item = Managers.Inven.Get(equipItemOk.ItemDbId);
		if (item == null)
			return;

		item.Equipped = equipItemOk.Equipped;
		Debug.Log("아이템 착용 변경!");

		UI_GameScene gameSceneUI = Managers.UI.SceneUI as UI_GameScene;
		gameSceneUI.InvenUI.RefreshUI();
		gameSceneUI.StatUI.RefreshUI();
	}

	public static void S_ChangeStatHandler(PacketSession session, IMessage packet)
	{
		S_ChangeStat itemList = (S_ChangeStat)packet;

		// TODO
	}

	public static void S_PingHandler(PacketSession session, IMessage packet)
	{
		C_Pong pongPacket = new C_Pong();
		//Debug.Log("[Server] PingCheck");
		Managers.Network.Send(pongPacket);
	}

	public static void S_RequestSendOkHandler(PacketSession session, IMessage packet)
    {
        S_RequestSendOk requestSendOK = (S_RequestSendOk)packet;

		if(requestSendOK.SendOK == 1)
		{
			// 패킷 전송 성공
			Managers.UI.ClosePopupUI();
            UICommonWaitPopup popup = Managers.UI.ShowPopupUI<UICommonWaitPopup>();
			popup.Text = "상대 응답 대기 중";
		}
		else
		{
			// 대결 신청 패킷 보내기 실패
			Managers.UI.ClosePopupUI();
			UICommonAnnouncePopup popup = Managers.UI.ShowPopupUI<UICommonAnnouncePopup>();
			popup.SetAnnounceText("상대가 로비에 존재하지 않습니다");
		}
	}

    public static void S_RequestDuelHandler(PacketSession session, IMessage packet)
    {
        S_RequestDuel requestDuel = (S_RequestDuel)packet;
        UILobbyDualRespondPopup popup = Managers.UI.ShowPopupUI<UILobbyDualRespondPopup>("UICommonRespondPopup");
		if(popup == null)
			Console.WriteLine("( UICommonRespondPopup )을 찾을 수 없습니다.");

		popup.SetDuelRequestAnnounce(requestDuel.FromId);
    }

    public static void S_RespondDuelHandler(PacketSession session, IMessage packet)
    {
        S_RespondDuel respenDuel = (S_RespondDuel)packet;

		bool battleStart = respenDuel.DuelOK == 1 ? true : false;

		if(battleStart)
        {
            Managers.UI.ClosePopupUI();
            UICommonAnnouncePopup popup = Managers.UI.ShowPopupUI<UICommonAnnouncePopup>();
            popup.SetAnnounceText("게임이 곧 시작됩니다");

			// 인게임으로 전환
			Managers.Scene.LoadScene(Define.Scene.Select);
        }
        else
		{
			Managers.UI.ClosePopupUI();
            UICommonAnnouncePopup popup = Managers.UI.ShowPopupUI<UICommonAnnouncePopup>();
			popup.SetAnnounceText("상대가 거절하였습니다");
		}
    }

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

            if (targetHPRatio <= 0f)
            {
                Managers.Job.Push(() => { battleScene.SetAnnounce($"{toName}은(는) 쓰려졌다.", true); });

                if (Managers.Player.MyTurn == false)
                    Managers.Job.Push(() => { battleScene.CurrentPokemonFallDown(); });
                else
                    Managers.Job.Push(() => { battleScene.SetAnnounce("상대 차례를 기다리는 중", true); });

                return true;
            }
        }

        return false;
    }

	static void BattleTurnInfo(RepeatedField<BattleInfo> battleInfoList, int skillId)
    {
		foreach (BattleInfo battleInfo in battleInfoList)
        {
            UIBattleScene battleScene = Managers.UI.SceneUI.GetComponent<UIBattleScene>();
            TargetType targetType = TargetType.End;
            string skillName = Managers.Data.SkillDict[skillId].name;
            string fromName = battleInfo.FromData.Name;
            string toName = battleInfo.ToData.Name;
            float targetHPRatio = Util.GetPokemonHPRatio(battleInfo.ToData);

            if (battleInfo.TargetType == TargetType.Oneself)
                targetType = Managers.Player.MyTurn ? TargetType.Oneself : TargetType.Enemy;
            else
                targetType = Managers.Player.MyTurn ? TargetType.Enemy : TargetType.Oneself;

            if (Managers.Player.MyTurn == false)
                Managers.Job.Push(() => { battleScene.SetAnnounce($"{fromName}의 {skillName}!", true); });

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

    static void ChechPokemonFallDown(UIBattleScene battleScene, string fallDownPokemonName, float hpRatio, bool isMyPokemon)
    {
        if (hpRatio > 0f)
            return;

        Managers.Job.Push(() => { battleScene.SetAnnounce($"{fallDownPokemonName}은(는) 쓰려졌다.", true); });
        if (Managers.Player.MyTurn)
        {
            if (isMyPokemon)
                Managers.Job.Push(() => { battleScene.CurrentPokemonFallDown(); });
            else
                Managers.Job.Push(() => { battleScene.SetAnnounce("상대 차례를 기다리는 중", true); });
        }
        else
        {
            if (isMyPokemon)
                Managers.Job.Push(() => { battleScene.SetAnnounce("상대 차례를 기다리는 중", true); });
            else
                Managers.Job.Push(() => { battleScene.CurrentPokemonFallDown(); });
        }
    }

    static string GetNoneAttackBattleAnnounce(BattleInfo battleInfo)
    {
        string valueTypeWord = "";
        string valueWord = battleInfo.SkillValue == 1 ? "" : "크게 ";
        string behaviorWord = "";

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
}