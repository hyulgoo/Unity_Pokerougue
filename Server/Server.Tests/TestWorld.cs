using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game.Room;
using Xunit;

// GameLogic·PlayerManager·DataManager가 정적 싱글턴이라 테스트를 병렬로 돌리지 않는다.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Server.Tests
{
    // DB와 소켓 없이 방 로직을 돌리기 위한 준비 코드.
    // 잡 큐는 테스트가 GameLogic.Instance.Update()를 직접 불러 한 스레드에서 비운다.
    internal static class TestWorld
    {
        // 테스트마다 데이터를 새로 읽어 앞 테스트가 바꾼 값이 남지 않게 한다.
        public static void LoadData()
        {
            ConfigManager.LoadConfig();
            DataManager.LoadData();
        }

        public static void Pump(int times = 3)
        {
            for (int i = 0; i < times; i++)
                GameLogic.Instance.Update();
        }

        // 로그인을 마치고 로비에 들어온 세션. 로그인 자체는 DB가 필요해 거치지 않는다.
        public static ClientSession EnterLobby(GameRoom lobby, string name)
        {
            ClientSession session = new ClientSession();
            LobbyPlayerInfo info = new LobbyPlayerInfo { PlayerDbId = 0, Name = name };
            LobbyPlayers(session).Add(info);
            session.HandleReEnterHandler(lobby.RoomId); // ServerState를 로비로 만든다
            lobby.SetPlayerBySession(session, info);
            return session;
        }

        // 대전 신청과 수락을 거쳐 두 세션을 새 전투방에 넣는다.
        public static GameRoom StartDuel(GameRoom lobby, ClientSession requester, ClientSession responder)
        {
            int requesterId = requester.MyPlayer.Id;
            lobby.RequestDuel(requesterId, responder.MyPlayer.Id);
            lobby.RespondDuel(responder.MyPlayer.Id, new C_RespondDuel { ToId = requesterId, DuelOK = 1 });
            Pump();
            return requester.MyPlayer.Room;
        }

        public static C_SelectPokemon Select(params int[] pokemonIds)
        {
            C_SelectPokemon packet = new C_SelectPokemon();
            packet.PokemonList.AddRange(pokemonIds);
            return packet;
        }

        public static int[] TwoPokemon()
        {
            return DataManager.PokemonDict.Keys.Take(2).ToArray();
        }

        // 두 사람이 포켓몬을 골라 전투가 시작된 방과 두 사람을 돌려준다. 둘 다 같은 두 마리(388, 392)를 고른다.
        public static (GameRoom room, ClientSession owner, ClientSession other) StartBattle()
        {
            LoadData();
            GameRoom lobby = GameLogic.Instance.Add();
            ClientSession a = EnterLobby(lobby, "a");
            ClientSession b = EnterLobby(lobby, "b");
            GameRoom room = StartDuel(lobby, a, b);

            int[] pokemon = TwoPokemon();
            room.SelectPokemon(a.MyPlayer.Id, Select(pokemon));
            room.SelectPokemon(b.MyPlayer.Id, Select(pokemon));

            return (room, a, b);
        }

        public static C_Turn Fight(int skillId, int playerIdInPacket = 0)
        {
            return new C_Turn
            {
                PlayerId = playerIdInPacket,
                TurnInfo = new TurnInfo { Action = ActionType.Fight, SkillId = skillId }
            };
        }

        // 아무것도 하지 않는 행동. 한쪽의 효과만 보고 싶을 때 상대에게 시킨다.
        public static C_Turn Pass()
        {
            return new C_Turn { TurnInfo = new TurnInfo { Action = ActionType.Pass } };
        }

        public static C_Turn Change(int pokemonId, int playerIdInPacket = 0)
        {
            return new C_Turn
            {
                PlayerId = playerIdInPacket,
                TurnInfo = new TurnInfo { Action = ActionType.Change, ChangePokemonId = pokemonId }
            };
        }

        // 두 사람이 행동을 고른다. 둘째 행동을 받는 순간 라운드가 처리된다.
        public static void Round(GameRoom room, ClientSession a, C_Turn aTurn, ClientSession b, C_Turn bTurn)
        {
            room.Turn(a.MyPlayer.Id, aTurn);
            room.Turn(b.MyPlayer.Id, bTurn);
        }

        // 전투 시작 전 준비용: 그 포켓몬을 맨 앞(출전 중)으로 옮긴다.
        public static void PutFirst(ClientSession session, int pokemonId)
        {
            List<PokemonData> pokemon = session.MyPlayer.Pokemon;
            int index = pokemon.FindIndex(p => p.Id == pokemonId);
            (pokemon[0], pokemon[index]) = (pokemon[index], pokemon[0]);
        }

        public static C_Turn Runaway(int playerIdInPacket = 0)
        {
            return new C_Turn
            {
                PlayerId = playerIdInPacket,
                TurnInfo = new TurnInfo { Action = ActionType.Runaway }
            };
        }

        // 세션이 보낼 패킷 중 그 종류만 순서대로 읽는다. 큐 형식: [size u16][msgId u16][body].
        public static List<T> Sent<T>(ClientSession session, MsgId id, Google.Protobuf.MessageParser<T> parser)
            where T : Google.Protobuf.IMessage<T>
        {
            FieldInfo field = typeof(ClientSession).GetField("_reserveQueue", BindingFlags.Instance | BindingFlags.NonPublic);
            List<System.ArraySegment<byte>> queue = (List<System.ArraySegment<byte>>)field.GetValue(session);

            List<T> result = new List<T>();
            foreach (System.ArraySegment<byte> buffer in queue)
            {
                ushort msgId = System.BitConverter.ToUInt16(buffer.Array, buffer.Offset + 2);
                if (msgId == (ushort)id)
                    result.Add(parser.ParseFrom(buffer.Array, buffer.Offset + 4, buffer.Count - 4));
            }
            return result;
        }

        // 세션이 보낼 패킷의 종류를 보낸 순서대로 돌려준다.
        public static List<MsgId> SentIds(ClientSession session)
        {
            FieldInfo field = typeof(ClientSession).GetField("_reserveQueue", BindingFlags.Instance | BindingFlags.NonPublic);
            List<System.ArraySegment<byte>> queue = (List<System.ArraySegment<byte>>)field.GetValue(session);
            return queue.Select(buffer => (MsgId)System.BitConverter.ToUInt16(buffer.Array, buffer.Offset + 2)).ToList();
        }

        // 양쪽의 연출이 끝났다고 알린다. 쓰러진 포켓몬이 없으면 다음 라운드가 시작된다.
        public static void EndTurn(GameRoom room, ClientSession a, ClientSession b)
        {
            room.TurnEnd(a.MyPlayer.Id);
            room.TurnEnd(b.MyPlayer.Id);
        }

        private static List<LobbyPlayerInfo> LobbyPlayers(ClientSession session)
        {
            PropertyInfo property = typeof(ClientSession).GetProperty("LobbyPlayers",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return (List<LobbyPlayerInfo>)property.GetValue(session);
        }
    }
}
