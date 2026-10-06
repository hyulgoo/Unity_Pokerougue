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
        // 전투가 마스터 데이터의 HP를 직접 깎으므로(PK-R02) 테스트마다 다시 읽는다.
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

        // 두 사람이 포켓몬을 골라 전투가 시작된 방과, 첫 턴의 주인·상대를 돌려준다.
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

            return a.MyPlayer.Id == room.CurrentTurnPlayerId ? (room, a, b) : (room, b, a);
        }

        public static C_Turn Fight(int skillId, int playerIdInPacket = 0)
        {
            return new C_Turn
            {
                PlayerId = playerIdInPacket,
                TurnInfo = new TurnInfo { Action = ActionType.Fight, SkillId = skillId }
            };
        }

        public static C_Turn Runaway(int playerIdInPacket = 0)
        {
            return new C_Turn
            {
                PlayerId = playerIdInPacket,
                TurnInfo = new TurnInfo { Action = ActionType.Runaway }
            };
        }

        // 양쪽의 연출이 끝났다고 알린다. 턴이 진행 중이었다면 턴 주인이 바뀐다.
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
