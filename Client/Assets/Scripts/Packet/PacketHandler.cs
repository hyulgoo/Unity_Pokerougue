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
			Managers.Player.Add(obj, myPlayer: false);

        Managers.UI.SetLobbyPlayer();
    }

	public static void S_DespawnHandler(PacketSession session, IMessage packet)
	{
		S_Despawn despawnPacket = packet as S_Despawn;
		foreach (int id in despawnPacket.ObjectIds)
			Managers.Player.Remove(id);

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
		Managers.Job.Push(() => 
		{
			Managers.Scene.LoadScene(Define.Scene.Lobby);
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
                for (int index = 0; index < loginPacket.Players.Count; index++)
                {
                    LobbyPlayerInfo info = loginPacket.Players[index];
                    if (index == 0)
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
        });

		Managers.Job.Excute();       
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
	}

	public static void S_PingHandler(PacketSession session, IMessage packet)
	{
		C_Pong pongPacket = new C_Pong();
		//Debug.Log("[Server] PingCheck");
		Managers.Network.Send(pongPacket);
	}
}