using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server;
using Server.Data;
using Server.DB;
using Server.Game;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

class PacketHandler
{
	public static void C_LoginHandler(PacketSession session, IMessage packet)
	{
		C_Login loginPacket = packet as C_Login;
		ClientSession clientSession = session as ClientSession;
		clientSession.HandleLogin(loginPacket);
	}

	public static void C_EnterGameHandler(PacketSession session, IMessage packet)
	{
		C_EnterGame enterGamePacket = (C_EnterGame)packet;
		ClientSession clientSession = (ClientSession)session;
		clientSession.HandleEnterGame(enterGamePacket);
	}

	public static void C_CreatePlayerHandler(PacketSession session, IMessage packet)
	{
		C_CreatePlayer createPlayerPacket = (C_CreatePlayer)packet;
		ClientSession clientSession = (ClientSession)session;
		clientSession.HandleCreatePlayer(createPlayerPacket);
	}

	public static void C_EquipItemHandler(PacketSession session, IMessage packet)
	{
		C_EquipItem equipPacket = (C_EquipItem)packet;
		ClientSession clientSession = (ClientSession)session;

		Player player = clientSession.MyPlayer;
        if (player == null)
        {
            Debug.Assert(false, "Fail To Found MyPlayer on C_EquipItemHandler");
        }

        GameRoom room = player.Room;
        if (room == null)
        {
            Debug.Assert(false, "Fail To Found MyPlayer Room on C_EquipItemHandler");
        }

        room.Push(room.HandleEquipItem, player, equipPacket);
	}

	public static void C_PongHandler(PacketSession session, IMessage packet)
	{
		ClientSession clientSession = (ClientSession)session;
		clientSession.HandlePong();
	}

	public static void C_RequestDuelHandler(PacketSession session, IMessage packet)
    {
        C_RequestDuel requestDuelPaceket = (C_RequestDuel)packet;
        ClientSession clientSession = (ClientSession)session;

		Player player = clientSession.MyPlayer;
        if (player == null)
        {
            Debug.Assert(false, "Fail To Found MyPlayer on C_RequestDuelHandler");
        }

        GameRoom room = player.Room;
        if (room == null)
        {
            Debug.Assert(false, "Fail To Found MyPlayer Room on C_RequestDuelHandler");
        }

        room.Push(room.RequestDuel, requestDuelPaceket.FromId, requestDuelPaceket.ToId);
    }

    public static void C_RespondDuelHandler(PacketSession session, IMessage packet)
    {
        C_RespondDuel respondDuelPacket = (C_RespondDuel)packet;
        ClientSession clientSession = (ClientSession)session;

        Player player = clientSession.MyPlayer;
        if (player == null)
        {
            Debug.Assert(false, "Fail To Found MyPlayer on C_RespondDuelHandler");
        }

        GameRoom room = player.Room;
        if (room == null)
        {
            Debug.Assert(false, "Fail To Found MyPlayer Room on C_RespondDuelHandler");
        }

        room.Push(room.RespondDuel, respondDuelPacket);
    }

    public static void C_SelectPokemonHandler(PacketSession session, IMessage packet)
    {
        C_SelectPokemon selectPokemonPacket = (C_SelectPokemon)packet;
        ClientSession clientSession = (ClientSession)session;

        Player player = clientSession.MyPlayer;
        if (player == null)
        {
            Debug.Assert(false, "Fail To Found MyPlayer on C_SelectPokemonHandler");
        }

        GameRoom room = player.Room;
        if (room == null)
        {
            Debug.Assert(false, "Fail To Found MyPlayer Room on C_SelectPokemonHandler");
        }

        for(int i = 0; i < selectPokemonPacket.PokemonList.Count; ++i)
            player.Pokemon.Add(DataManager.PokemonDict[selectPokemonPacket.PokemonList[i]]);

        room.Push(room.SelectPokemon, selectPokemonPacket.PlayerId);
    }

    public static void C_TurnHandler(PacketSession session, IMessage packet)
    {
        C_Turn turnPacket = (C_Turn)packet;
        ClientSession clientSession = (ClientSession)session;

        Player player = clientSession.MyPlayer;
        if (player == null)
        {
            Debug.Assert(false, "Fail To Found MyPlayer on C_TurnHandler");
        }

        GameRoom room = player.Room;
        if (room == null)
        {
            Debug.Assert(false, "Fail To Found MyPlayer Room on C_TurnHandler");
        }

        room.Push(room.Turn, turnPacket);
    }
    public static void C_TurnEndHandler(PacketSession session, IMessage packet)
    {
        C_TurnEnd turnPacket = (C_TurnEnd)packet;
        ClientSession clientSession = (ClientSession)session;

        Player player = clientSession.MyPlayer;
        if (player == null)
        {
            Debug.Assert(false, "Fail To Found MyPlayer on C_TurnEndHandler");
        }

        GameRoom room = player.Room;
        if (room == null)
        {
            Debug.Assert(false, "Fail To Found MyPlayer Room on C_TurnEndHandler");
        }

        room.Push(room.TurnEnd, turnPacket.PlayerId);
    }

    public static void C_ChangePokemonHandler(PacketSession session, IMessage packet)
    {
        C_ChangePokemon changePokemonPacket = (C_ChangePokemon)packet;
        ClientSession clientSession = (ClientSession)session;

        Player player = clientSession.MyPlayer;
        if (player == null)
        {
            Debug.Assert(false, "Fail To Found MyPlayer on C_ChangePokemonHandler");
        }

        GameRoom room = player.Room;
        if (room == null)
        {
            Debug.Assert(false, "Fail To Found MyPlayer Room on C_ChangePokemonHandler");
        }

        room.Push(room.ChangePokemon, changePokemonPacket.PlayerId, changePokemonPacket.ChangePokemonId);
    }
}
