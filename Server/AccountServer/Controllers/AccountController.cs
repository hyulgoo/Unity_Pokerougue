using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AccountServer.DB;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SharedDB;

namespace AccountServer.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[EnableRateLimiting(Startup.AccountRateLimitPolicy)]
	public class AccountController : ControllerBase
	{
		// 토큰 유효 시간(초)
		const int TokenLifetimeSeconds = 600;

		// PasswordHasher가 만드는 해시의 최소 길이(형식 표시 1 + 솔트 16 + 키 32)
		const int MinHashBytes = 49;

		static readonly PasswordHasher<AccountDb> _passwordHasher = new PasswordHasher<AccountDb>();

		AppDbContext _context;
		SharedDbContext _shared;

		public AccountController(AppDbContext context, SharedDbContext shared)
		{
			_context = context;
			_shared = shared;
		}

		// 저장된 값이 PasswordHasher가 만든 해시인가. 아니면 해시를 쓰기 전에 평문으로 저장된 값이다.
		static bool IsHashed(string stored)
		{
			if (string.IsNullOrEmpty(stored))
				return false;

			try
			{
				// 첫 바이트는 해시 형식 표시다(0x00, 0x01).
				byte[] decoded = Convert.FromBase64String(stored);
				return decoded.Length >= MinHashBytes && decoded[0] <= 0x01;
			}
			catch (FormatException)
			{
				return false;
			}
		}

		// 평문으로 저장돼 있던 계정은 맞게 입력했을 때 해시로 바꿔 저장한다.
		// 해시로 저장된 계정에는 평문 비교를 하지 않는다. 하면 DB에서 본 해시 문자열을 그대로 넣어 로그인할 수 있다.
		bool VerifyPassword(AccountDb account, string password)
		{
			if (IsHashed(account.Password))
				return _passwordHasher.VerifyHashedPassword(account, account.Password, password) !=
				       PasswordVerificationResult.Failed;

			if (account.Password != password)
				return false;

			account.Password = _passwordHasher.HashPassword(account, password);
			_context.SaveChangesEx();
			return true;
		}

		[HttpPost]
		[Route("create")]
		public CreateAccountPacketRes CreateAccount([FromBody] CreateAccountPacketReq req)
		{
			CreateAccountPacketRes res = new CreateAccountPacketRes();

			if (string.IsNullOrEmpty(req.AccountName) || string.IsNullOrEmpty(req.Password))
				return res;

			AccountDb account = _context.Accounts
									.AsNoTracking()
									.Where(a => a.AccountName == req.AccountName)
									.FirstOrDefault();

			if (account == null)
			{
				AccountDb newAccount = new AccountDb() { AccountName = req.AccountName };
				newAccount.Password = _passwordHasher.HashPassword(newAccount, req.Password);
				_context.Accounts.Add(newAccount);

				bool success = _context.SaveChangesEx();
				res.CreateOk = success;
			}
			else
			{
				res.CreateOk = false;
			}

			return res;
		}

		[HttpPost]
		[Route("login")]
		public LoginAccountPacketRes LoginAccount([FromBody] LoginAccountPacketReq req)
		{
			LoginAccountPacketRes res = new LoginAccountPacketRes();

			if (string.IsNullOrEmpty(req.AccountName) || string.IsNullOrEmpty(req.Password))
				return res;

			// 비밀번호는 해시라 SQL에서 비교할 수 없다. 계정을 찾은 뒤 코드에서 확인한다.
			AccountDb account = _context.Accounts
				.Where(a => a.AccountName == req.AccountName)
				.FirstOrDefault();

			if (account == null || !VerifyPassword(account, req.Password))
			{
				res.LoginOk = false;
			}
			else
			{
				res.LoginOk = true;

				// 토큰 발급. 게임 서버가 이 값과 만료 시각을 대조하므로 예측할 수 없는 난수를 쓴다.
				DateTime expired = DateTime.UtcNow.AddSeconds(TokenLifetimeSeconds);
				int token = RandomNumberGenerator.GetInt32(Int32.MinValue, Int32.MaxValue);

				TokenDb tokenDb = _shared.Tokens.Where(t => t.AccountDbId == account.AccountDbId).FirstOrDefault();
				if (tokenDb != null)
				{
					tokenDb.Token = token;
					tokenDb.Expired = expired;
					_shared.SaveChangesEx();
				}
				else
				{
					tokenDb = new TokenDb()
					{
						AccountDbId = account.AccountDbId,
						Token = token,
						Expired = expired
					};
					_shared.Add(tokenDb);
					_shared.SaveChangesEx();
				}

				res.AccountId = account.AccountDbId;
				res.Token = tokenDb.Token;
				res.ServerList = new List<ServerInfo>();

				foreach (ServerDb serverDb in _shared.Servers)
				{
					res.ServerList.Add(new ServerInfo()
					{
						Name = serverDb.Name,
						IpAddress = serverDb.IpAddress,
						Port = serverDb.Port,
						BusyScore = serverDb.BusyScore
					});
				}
			}

			return res;
		}
	}
}
