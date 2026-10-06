using System;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Game;
using Server.Packet;
using ServerCore;
using Xunit;

namespace Server.Tests
{
    // 수신 경로와 핸들러 입구의 방어 (PK-R40, PK-R08, PK-R05)
    public class PacketGuardTests
    {
        private static byte[] Header(ushort size, ushort id)
        {
            byte[] bytes = new byte[4];
            BitConverter.GetBytes(size).CopyTo(bytes, 0);
            BitConverter.GetBytes(id).CopyTo(bytes, 2);
            return bytes;
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        public void 헤더보다_작은_크기를_적은_패킷은_음수를_돌려준다(ushort size)
        {
            ClientSession session = new ClientSession();

            int processed = session.OnRecv(new ArraySegment<byte>(Header(size, 0xFFFF)));

            Assert.True(processed < 0);
        }

        [Fact]
        public void 알_수_없는_ID의_패킷은_크기만큼_건너뛴다()
        {
            ClientSession session = new ClientSession();

            int processed = session.OnRecv(new ArraySegment<byte>(Header(4, 0xFFFF)));

            Assert.Equal(4, processed);
        }

        [Fact]
        public void 다_도착하지_않은_패킷은_처리하지_않고_기다린다()
        {
            ClientSession session = new ClientSession();

            int processed = session.OnRecv(new ArraySegment<byte>(Header(100, 0xFFFF)));

            Assert.Equal(0, processed);
        }

        public static TheoryData<Action<PacketSession, IMessage>, IMessage> 로그인_전_패킷 =>
            new TheoryData<Action<PacketSession, IMessage>, IMessage>
            {
                { PacketHandler.C_EquipItemHandler, new C_EquipItem() },
                { PacketHandler.C_RequestDuelHandler, new C_RequestDuel { FromId = 1, ToId = 2 } },
                { PacketHandler.C_RespondDuelHandler, new C_RespondDuel { FromId = 1, ToId = 2, DuelOK = 1 } },
                { PacketHandler.C_SelectPokemonHandler, new C_SelectPokemon { PlayerId = 1 } },
                { PacketHandler.C_TurnHandler, new C_Turn { PlayerId = 1 } },
                { PacketHandler.C_TurnEndHandler, new C_TurnEnd { PlayerId = 1 } },
                { PacketHandler.C_ChangePokemonHandler, new C_ChangePokemon { PlayerId = 1, ChangePokemonId = 1 } }
            };

        [Theory]
        [MemberData(nameof(로그인_전_패킷))]
        public void 로그인_전_세션의_대전_패킷은_예외_없이_무시된다(Action<PacketSession, IMessage> handler,
            IMessage packet)
        {
            ClientSession session = new ClientSession();

            Exception exception = Record.Exception(() => handler(session, packet));

            Assert.Null(exception);
            Assert.Null(session.MyPlayer);
        }

        [Fact]
        public void 잡_하나의_예외가_다음_잡을_막지_않는다()
        {
            JobSerializer jobs = new JobSerializer();
            bool ran = false;
            jobs.Push(() => throw new InvalidOperationException("테스트용 예외"));
            jobs.Push(() => ran = true);

            Exception exception = Record.Exception(() => jobs.Flush());

            Assert.Null(exception);
            Assert.True(ran);
        }

        [Fact]
        public void 타이머_잡의_예외도_밖으로_나가지_않는다()
        {
            JobSerializer jobs = new JobSerializer();
            jobs.PushAfter(0, () => throw new InvalidOperationException("테스트용 예외"));

            Exception exception = Record.Exception(() => jobs.Flush());

            Assert.Null(exception);
        }
    }
}
