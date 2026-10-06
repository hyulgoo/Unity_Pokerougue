using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Game;
using ServerCore;
using GameRoom = Server.Game.Room.GameRoom;

namespace Server.Packet
{
    internal abstract class PacketHandler
    {
        // 세션 → 플레이어 → 방. 로그인 전이거나 방을 옮기는 중이면 실패한다.
        // 행위자는 항상 여기서 얻은 플레이어다. 패킷에 실린 플레이어 ID는 믿지 않는다.
        private static bool TryGetPlayerRoom(PacketSession session, out Player player, out GameRoom room)
        {
            player = (session as ClientSession)?.MyPlayer;
            room = player?.Room;
            return room != null;
        }

        public static void C_LoginHandler(PacketSession session, IMessage packet)
        {
            C_Login loginPacket = packet as C_Login;
            if (session is ClientSession clientSession) clientSession.HandleLogin(loginPacket);
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
            if (!TryGetPlayerRoom(session, out Player player, out GameRoom room))
                return;

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
            if (!TryGetPlayerRoom(session, out Player player, out GameRoom room))
                return;

            room.Push(room.RequestDuel, player.Id, requestDuelPaceket.ToId);
        }

        public static void C_RespondDuelHandler(PacketSession session, IMessage packet)
        {
            C_RespondDuel respondDuelPacket = (C_RespondDuel)packet;
            if (!TryGetPlayerRoom(session, out Player player, out GameRoom room))
                return;

            room.Push(room.RespondDuel, player.Id, respondDuelPacket);
        }

        public static void C_SelectPokemonHandler(PacketSession session, IMessage packet)
        {
            C_SelectPokemon selectPokemonPacket = (C_SelectPokemon)packet;
            if (!TryGetPlayerRoom(session, out Player player, out GameRoom room))
                return;

            // 선택 목록의 검증과 반영은 방의 잡 안에서 한다.
            room.Push(room.SelectPokemon, player.Id, selectPokemonPacket);
        }

        public static void C_TurnHandler(PacketSession session, IMessage packet)
        {
            C_Turn turnPacket = (C_Turn)packet;
            if (!TryGetPlayerRoom(session, out Player player, out GameRoom room))
                return;

            room.Push(room.Turn, player.Id, turnPacket);
        }

        public static void C_TurnEndHandler(PacketSession session, IMessage packet)
        {
            if (!TryGetPlayerRoom(session, out Player player, out GameRoom room))
                return;

            room.Push(room.TurnEnd, player.Id);
        }

        public static void C_ChangePokemonHandler(PacketSession session, IMessage packet)
        {
            C_ChangePokemon changePokemonPacket = (C_ChangePokemon)packet;
            if (!TryGetPlayerRoom(session, out Player player, out GameRoom room))
                return;

            room.Push(room.ChangePokemon, player.Id, changePokemonPacket.ChangePokemonId);
        }
    }
}