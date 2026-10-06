using System;
using Google.Protobuf.Protocol;
using Server.Game.Room;
using Server.Packet;
using Xunit;

namespace Server.Tests
{
    // 로비에서의 행위자 검증 (PK-R05, PK-R13, PK-R17)
    public class LobbyAuthorityTests
    {
        private static GameRoom NewLobby()
        {
            TestWorld.LoadData();
            return GameLogic.Instance.Add();
        }

        [Fact]
        public void 없는_플레이어_ID로_신청하거나_응답해도_예외가_나지_않는다()
        {
            GameRoom lobby = NewLobby();
            ClientSession a = TestWorld.EnterLobby(lobby, "a");

            Exception exception = Record.Exception(() =>
            {
                lobby.RequestDuel(a.MyPlayer.Id, 123456);
                lobby.RequestDuel(123456, a.MyPlayer.Id);
                lobby.RespondDuel(a.MyPlayer.Id, new C_RespondDuel { ToId = 123456, DuelOK = 1 });
                lobby.RespondDuel(123456, new C_RespondDuel { ToId = a.MyPlayer.Id, DuelOK = 1 });
                TestWorld.Pump();
            });

            Assert.Null(exception);
            Assert.Equal(PlayerServerState.ServerStateLobby, a.ServerState);
        }

        [Fact]
        public void 신청과_수락을_거치면_두_사람이_전투방으로_옮겨진다()
        {
            GameRoom lobby = NewLobby();
            ClientSession a = TestWorld.EnterLobby(lobby, "a");
            ClientSession b = TestWorld.EnterLobby(lobby, "b");

            GameRoom battle = TestWorld.StartDuel(lobby, a, b);

            Assert.NotSame(lobby, battle);
            Assert.Same(battle, b.MyPlayer.Room);
            Assert.Equal(PlayerServerState.ServerStateGame, a.ServerState);
            Assert.Equal(PlayerServerState.ServerStateGame, b.ServerState);
        }

        [Fact]
        public void 신청받지_않은_수락은_무시된다()
        {
            GameRoom lobby = NewLobby();
            ClientSession a = TestWorld.EnterLobby(lobby, "a");
            ClientSession b = TestWorld.EnterLobby(lobby, "b");

            lobby.RespondDuel(b.MyPlayer.Id, new C_RespondDuel { ToId = a.MyPlayer.Id, DuelOK = 1 });
            TestWorld.Pump();

            Assert.Equal(PlayerServerState.ServerStateLobby, a.ServerState);
            Assert.Equal(PlayerServerState.ServerStateLobby, b.ServerState);
        }

        [Fact]
        public void 제삼자가_남의_ID를_적어_수락해도_대전이_시작되지_않는다()
        {
            GameRoom lobby = NewLobby();
            ClientSession a = TestWorld.EnterLobby(lobby, "a");
            ClientSession b = TestWorld.EnterLobby(lobby, "b");
            ClientSession c = TestWorld.EnterLobby(lobby, "c");
            lobby.RequestDuel(a.MyPlayer.Id, b.MyPlayer.Id);

            // c가 "b가 a의 신청을 수락했다"는 패킷을 보낸다. 핸들러는 응답자를 c의 세션에서 얻는다.
            PacketHandler.C_RespondDuelHandler(c,
                new C_RespondDuel { FromId = b.MyPlayer.Id, ToId = a.MyPlayer.Id, DuelOK = 1 });
            TestWorld.Pump();

            Assert.Equal(PlayerServerState.ServerStateLobby, a.ServerState);
            Assert.Equal(PlayerServerState.ServerStateLobby, b.ServerState);
            Assert.Equal(PlayerServerState.ServerStateLobby, c.ServerState);
        }

        [Fact]
        public void 신청_패킷의_FromId를_남의_ID로_적어도_신청자는_세션의_플레이어다()
        {
            GameRoom lobby = NewLobby();
            ClientSession a = TestWorld.EnterLobby(lobby, "a");
            ClientSession b = TestWorld.EnterLobby(lobby, "b");
            ClientSession c = TestWorld.EnterLobby(lobby, "c");

            // c가 "a가 b에게 신청했다"는 패킷을 보낸다. 실제 신청자는 c로 기록된다.
            PacketHandler.C_RequestDuelHandler(c, new C_RequestDuel { FromId = a.MyPlayer.Id, ToId = b.MyPlayer.Id });
            TestWorld.Pump();

            // b가 a의 신청을 수락해도 그런 신청은 없다.
            lobby.RespondDuel(b.MyPlayer.Id, new C_RespondDuel { ToId = a.MyPlayer.Id, DuelOK = 1 });
            TestWorld.Pump();
            Assert.Equal(PlayerServerState.ServerStateLobby, a.ServerState);

            // c의 신청을 수락하면 c와 b가 시작한다.
            lobby.RespondDuel(b.MyPlayer.Id, new C_RespondDuel { ToId = c.MyPlayer.Id, DuelOK = 1 });
            TestWorld.Pump();
            Assert.Equal(PlayerServerState.ServerStateGame, b.ServerState);
            Assert.Equal(PlayerServerState.ServerStateGame, c.ServerState);
            Assert.Equal(PlayerServerState.ServerStateLobby, a.ServerState);
        }

        [Fact]
        public void 한_사람이_두_신청을_연달아_수락해도_대전은_하나만_시작된다()
        {
            GameRoom lobby = NewLobby();
            ClientSession a = TestWorld.EnterLobby(lobby, "a");
            ClientSession b = TestWorld.EnterLobby(lobby, "b");
            ClientSession c = TestWorld.EnterLobby(lobby, "c");
            lobby.RequestDuel(a.MyPlayer.Id, b.MyPlayer.Id);
            lobby.RequestDuel(c.MyPlayer.Id, b.MyPlayer.Id);

            lobby.RespondDuel(b.MyPlayer.Id, new C_RespondDuel { ToId = a.MyPlayer.Id, DuelOK = 1 });
            lobby.RespondDuel(b.MyPlayer.Id, new C_RespondDuel { ToId = c.MyPlayer.Id, DuelOK = 1 });
            TestWorld.Pump();

            Assert.Equal(PlayerServerState.ServerStateGame, a.ServerState);
            Assert.Equal(PlayerServerState.ServerStateGame, b.ServerState);
            Assert.Equal(PlayerServerState.ServerStateLobby, c.ServerState);
            Assert.Same(a.MyPlayer.Room, b.MyPlayer.Room);
        }

        [Fact]
        public void 같은_방에_입장을_다시_요청해도_플레이어가_늘지_않는다()
        {
            GameRoom lobby = NewLobby();
            ClientSession a = TestWorld.EnterLobby(lobby, "a");
            var first = a.MyPlayer;

            lobby.SetPlayerBySession(a, new LobbyPlayerInfo { Name = "a" });

            Assert.Same(first, a.MyPlayer);
        }
    }
}
