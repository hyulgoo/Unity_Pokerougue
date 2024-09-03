using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerCore;
using System;
using System.Drawing;
using UnityEditor;
using UnityEngine;

class PacketHandler
{
	public static void S_EnterGameHandler(PacketSession session, IMessage packet)
	{
		S_EnterGame enterGamePacket = packet as S_EnterGame;
		Managers.Object.Add(enterGamePacket.Player, myPlayer: true);
		Managers.UI.SetLobbyPlayer();
	}

	public static void S_LeaveGameHandler(PacketSession session, IMessage packet)
	{
		S_LeaveGame leaveGameHandler = packet as S_LeaveGame;
		Managers.Object.Clear();
    }

	public static void S_SpawnHandler(PacketSession session, IMessage packet)
	{
		S_Spawn spawnPacket = packet as S_Spawn;
		foreach (ObjectInfo obj in spawnPacket.Objects)
		{
			Managers.Object.Add(obj, myPlayer: false);
        }

        Managers.UI.SetLobbyPlayer();
    }

	public static void S_DespawnHandler(PacketSession session, IMessage packet)
	{
		S_Despawn despawnPacket = packet as S_Despawn;
		foreach (int id in despawnPacket.ObjectIds)
		{
			Managers.Object.Remove(id);
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

					Managers.Object.Add(playerinfo, myPlayer: false);
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
            UI_WaitingForRespondPopup popup = Managers.UI.ShowPopupUI<UI_WaitingForRespondPopup>();
			popup.Text = "상대 응답 대기 중";
		}
		else
		{
			// 대결 신청 패킷 보내기 실패
			Managers.UI.ClosePopupUI();
			UI_AnnouncePopup popup = Managers.UI.ShowPopupUI<UI_AnnouncePopup>();
			popup.SetAnnounceText("상대가 로비에 존재하지 않습니다");
		}
	}

    public static void S_RequestDuelHandler(PacketSession session, IMessage packet)
    {
        S_RequestDuel requestDuel = (S_RequestDuel)packet;
        UI_DualRespondPopup popup = Managers.UI.ShowPopupUI<UI_DualRespondPopup>("UI_AcceptDenyPopup");
		popup.SetApplyDuelAnnounce(requestDuel.RequestId);
    }

    public static void S_RespondDuelHandler(PacketSession session, IMessage packet)
    {
        S_RespondDuel respenDuel = (S_RespondDuel)packet;

		bool letsDuel = respenDuel.DuelOK == 1 ? true : false;

		if(letsDuel)
        {
            Managers.UI.ClosePopupUI();
            UI_AnnouncePopup popup = Managers.UI.ShowPopupUI<UI_AnnouncePopup>();
            popup.SetAnnounceText("게임이 곧 시작됩니다");

			// 인게임으로 전환
			Managers.Scene.LoadScene(Define.Scene.Select);
        }
        else
		{
			Managers.UI.ClosePopupUI();
            UI_AnnouncePopup popup = Managers.UI.ShowPopupUI<UI_AnnouncePopup>();
			popup.SetAnnounceText("상대가 거절하였습니다");
		}
    }

    public static void S_StartBattleHandler(PacketSession session, IMessage packet)
    {
        S_StartBattle startbattle = (S_StartBattle)packet;

		Managers.Scene.LoadScene(Define.Scene.Battle);

        Managers.Object.Add(startbattle.MyInfo, myPlayer: true);
		Managers.Object.Add(startbattle.EnemyInfo, myPlayer: false);

		Managers.Object.MyPlayer.SetMonsterData(startbattle.MyMst);
        Managers.Object.Enemy.SetMonsterData(startbattle.EnemyMst);
		Managers.Object.ArenaType = startbattle.ArenaType;
		Managers.Object.MyTurn = startbattle.MyTurn == 1 ? true : false;
    }

    public static void S_TurnBattleHandler(PacketSession session, IMessage packet)
    {
        S_TurnBattle battle = (S_TurnBattle)packet;
		bool reverse = Managers.Object.MyPlayer.Id == battle.PlayerId ? true : false;
		Battle(battle, reverse);
    }

	static void Battle(S_TurnBattle battle, bool reverse)
	{
		// 상태이상에 의한 턴 정보
		for (int i = 0; i < battle.TurnInfo.Count; ++i)
        {
            BattleInfo info = battle.TurnInfo[i];
			PokemonInfo pokeinfo = reverse ? info.EnemyInfo : info.MyInfo;
            string targetName = reverse ? Managers.Object.Enemy.GetCurMonsterName() : Managers.Object.MyPlayer.GetCurMonsterName();
            string announce = "";

			if (info.SkillType == SkillType.Dot)
			{
                string skillName = Managers.Data.SkillDict[info.StateInfo.SkillId].name;
                announce = $"{targetName}은(는) {skillName}에 의해 지속데미지를 받고있다.";
                Managers.Job.Push(() => { Managers.UI.BattleScene.SetAnnounce(announce); });
                Managers.Job.Push(() => { Managers.UI.BattleScene.SetHPBar(pokeinfo.Hp, isEnemy: reverse); });
            }
			else if(info.SkillType == SkillType.StatusEffect)
			{
                announce = $"{targetName}은(는) 지속데미지를 받고있다.";
                Managers.Job.Push(() => { Managers.UI.BattleScene.SetAnnounce(announce); });
                Managers.Job.Push(() => { Managers.UI.BattleScene.SetHPBar(pokeinfo.Hp, isEnemy: reverse); });
            }
        }
		 
		// 전투에 의한 턴 정보
		for(int i = 0; i < battle.Info.Count; ++i)
        {
            BattleInfo info = battle.Info[i];
            string announce = "";
            if (info.StateInfo.IsMiss)
            {
                string targetName = reverse ? Managers.Object.Enemy.GetCurMonsterName() : Managers.Object.MyPlayer.GetCurMonsterName();

                announce = $"{targetName}의 공격은 빗나갔다!";

                Managers.Job.Push(() => { Managers.UI.BattleScene.SetAnnounce(announce); });
                return;
            }

            if (info.TargetType == TargetType.Oneself)
            {
				PokemonInfo pokeinfo = reverse ? info.EnemyInfo : info.MyInfo;
                string targetName = reverse ? Managers.Object.Enemy.GetCurMonsterName() : Managers.Object.MyPlayer.GetCurMonsterName();

                announce = $"{targetName}은(는) 반동으로 인해 데미지를 입었다.";
                Managers.Job.Push(() => { Managers.UI.BattleScene.SetAnnounce(announce); });
                Managers.Job.Push(() => { Managers.UI.BattleScene.SetHPBar(pokeinfo.Hp, isEnemy: !reverse); });
            }
            else
            {
                PokemonInfo pokeinfo = reverse ? info.MyInfo : info.EnemyInfo;
                if (info.Effective != EffectiveType.Commoneffect)
                {
                    announce = info.Effective == EffectiveType.Effective ? "효과는 굉장했다." : "효과가 별로인듯 하다.";
                    Managers.Job.Push(() => { Managers.UI.BattleScene.SetAnnounce(announce); });
                }
                Managers.Job.Push(() => { Managers.UI.BattleScene.SetHPBar(pokeinfo.Hp, isEnemy: reverse); });
            }
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
    public static void S_TurnRunawayHandler(PacketSession session, IMessage packet)
    {
        S_TurnRunaway runaway = (S_TurnRunaway)packet;
    }
    public static void S_TurnPassHandler(PacketSession session, IMessage packet)
    {
        S_TurnPass turn = (S_TurnPass)packet;
    }
}