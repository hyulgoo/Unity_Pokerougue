using System;
using System.Linq;
using AccountServer.Controllers;
using Microsoft.EntityFrameworkCore;
using SharedDB;
using Xunit;
using AccountDb = AccountServer.DB.AccountDb;
using AccountDbContext = AccountServer.DB.AppDbContext;

namespace Server.Tests
{
    // AccountServer의 비밀번호 저장과 토큰 발급 (PK-R19, PK-R42).
    // SQL Server 대신 EF Core의 메모리 DB를 쓴다. 요청 제한은 미들웨어라 여기서 다루지 않는다.
    public class AccountControllerTests
    {
        private readonly AccountDbContext _accounts;
        private readonly SharedDbContext _shared;
        private readonly AccountController _controller;

        public AccountControllerTests()
        {
            string name = Guid.NewGuid().ToString();
            _accounts = new AccountDbContext(
                new DbContextOptionsBuilder<AccountDbContext>().UseInMemoryDatabase("account-" + name).Options);
            _shared = new SharedDbContext(
                new DbContextOptionsBuilder<SharedDbContext>().UseInMemoryDatabase("shared-" + name).Options);
            _controller = new AccountController(_accounts, _shared);
        }

        private bool Create(string account, string password)
        {
            return _controller.CreateAccount(new CreateAccountPacketReq { AccountName = account, Password = password })
                .CreateOk;
        }

        private LoginAccountPacketRes Login(string account, string password)
        {
            return _controller.LoginAccount(new LoginAccountPacketReq { AccountName = account, Password = password });
        }

        private string StoredPassword(string account)
        {
            return _accounts.Accounts.AsNoTracking().Single(a => a.AccountName == account).Password;
        }

        [Fact]
        public void 계정을_만들면_비밀번호가_평문으로_저장되지_않는다()
        {
            Assert.True(Create("hg", "pass1234"));

            string stored = StoredPassword("hg");
            Assert.NotEqual("pass1234", stored);
            Assert.DoesNotContain("pass1234", stored);
        }

        [Fact]
        public void 같은_비밀번호라도_저장되는_값은_계정마다_다르다()
        {
            Create("a", "pass1234");
            Create("b", "pass1234");

            Assert.NotEqual(StoredPassword("a"), StoredPassword("b"));
        }

        [Fact]
        public void 맞는_비밀번호로_로그인된다()
        {
            Create("hg", "pass1234");

            Assert.True(Login("hg", "pass1234").LoginOk);
        }

        [Theory]
        [InlineData("hg", "wrong")]
        [InlineData("nobody", "pass1234")]
        [InlineData("hg", "")]
        [InlineData("hg", null)]
        [InlineData("", "pass1234")]
        [InlineData(null, "pass1234")]
        public void 틀린_비밀번호와_없는_계정과_빈_입력은_거절된다(string account, string password)
        {
            Create("hg", "pass1234");

            Assert.False(Login(account, password).LoginOk);
        }

        [Fact]
        public void 저장된_해시_문자열을_비밀번호로_넣어도_로그인되지_않는다()
        {
            Create("hg", "pass1234");

            Assert.False(Login("hg", StoredPassword("hg")).LoginOk);
        }

        [Theory]
        [InlineData("", "pass1234")]
        [InlineData(null, "pass1234")]
        [InlineData("hg", "")]
        [InlineData("hg", null)]
        public void 빈_계정명이나_빈_비밀번호로는_계정을_만들_수_없다(string account, string password)
        {
            Assert.False(Create(account, password));
            Assert.Empty(_accounts.Accounts);
        }

        [Fact]
        public void 이미_있는_계정명으로는_만들_수_없다()
        {
            Create("hg", "pass1234");

            Assert.False(Create("hg", "other"));
            Assert.True(Login("hg", "pass1234").LoginOk);
        }

        [Fact]
        public void 평문으로_저장돼_있던_계정은_로그인할_때_해시로_바뀐다()
        {
            _accounts.Accounts.Add(new AccountDb { AccountName = "old", Password = "legacy" });
            _accounts.SaveChanges();

            Assert.False(Login("old", "wrong").LoginOk);
            Assert.Equal("legacy", StoredPassword("old"));

            Assert.True(Login("old", "legacy").LoginOk);
            Assert.NotEqual("legacy", StoredPassword("old"));
            Assert.True(Login("old", "legacy").LoginOk);
        }

        [Fact]
        public void 로그인하면_10분_뒤에_만료되는_토큰이_발급된다()
        {
            Create("hg", "pass1234");
            DateTime before = DateTime.UtcNow;

            LoginAccountPacketRes res = Login("hg", "pass1234");

            TokenDb token = _shared.Tokens.AsNoTracking().Single(t => t.AccountDbId == res.AccountId);
            Assert.Equal(res.Token, token.Token);
            Assert.InRange(token.Expired, before.AddSeconds(599), DateTime.UtcNow.AddSeconds(601));

            // 게임 서버의 판정을 통과한다. 고치기 전에는 만료 시각이 발급 시각과 같아 항상 거절됐다.
            Assert.True(ClientSession.IsLoginTokenValid(token, res.Token, DateTime.UtcNow));
        }

        [Fact]
        public void 다시_로그인하면_토큰이_바뀌고_행은_하나로_유지된다()
        {
            Create("hg", "pass1234");

            int first = Login("hg", "pass1234").Token;
            int second = Login("hg", "pass1234").Token;

            Assert.NotEqual(first, second);
            Assert.Single(_shared.Tokens);
        }

        [Fact]
        public void 로그인에_실패하면_토큰이_발급되지_않는다()
        {
            Create("hg", "pass1234");

            LoginAccountPacketRes res = Login("hg", "wrong");

            Assert.Equal(0, res.Token);
            Assert.Empty(_shared.Tokens);
        }
    }
}
