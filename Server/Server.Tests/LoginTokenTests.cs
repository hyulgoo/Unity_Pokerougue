using System;
using Google.Protobuf.Protocol;
using SharedDB;
using Xunit;

namespace Server.Tests
{
    // 게임 서버 로그인의 토큰 판정 (PK-R18).
    // SharedDB에서 토큰 행을 읽는 부분은 SQL Server가 필요해 여기서 다루지 않는다.
    public class LoginTokenTests
    {
        private static readonly DateTime Now = new DateTime(2026, 10, 7, 0, 0, 0, DateTimeKind.Utc);

        private static TokenDb Token(int value, DateTime expired)
        {
            return new TokenDb { AccountDbId = 1, Token = value, Expired = expired };
        }

        [Fact]
        public void 값이_같고_만료_전인_토큰은_통과한다()
        {
            Assert.True(ClientSession.IsLoginTokenValid(Token(1234, Now.AddSeconds(1)), 1234, Now));
        }

        [Fact]
        public void 발급된_토큰이_없으면_거절된다()
        {
            Assert.False(ClientSession.IsLoginTokenValid(null, 0, Now));
        }

        [Fact]
        public void 값이_다른_토큰은_거절된다()
        {
            Assert.False(ClientSession.IsLoginTokenValid(Token(1234, Now.AddSeconds(600)), 1235, Now));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void 만료된_토큰은_값이_같아도_거절된다(int secondsFromNow)
        {
            Assert.False(ClientSession.IsLoginTokenValid(Token(1234, Now.AddSeconds(secondsFromNow)), 1234, Now));
        }

        [Fact]
        public void 로그인_패킷은_계정_ID와_토큰을_싣는다()
        {
            C_Login sent = new C_Login { UniqueId = "x", AccountId = 7, Token = -42 };

            C_Login received = C_Login.Parser.ParseFrom(Google.Protobuf.MessageExtensions.ToByteArray(sent));

            Assert.Equal(7, received.AccountId);
            Assert.Equal(-42, received.Token);
        }
    }
}
