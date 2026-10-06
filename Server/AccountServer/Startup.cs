using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using AccountServer.DB;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedDB;

namespace AccountServer
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// 계정 생성·로그인 요청 제한 정책의 이름. 접속 주소 하나당 1분에 10번까지 받는다.
		public const string AccountRateLimitPolicy = "account";
		const int AccountRequestsPerMinute = 10;

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			// 비밀번호를 무한정 시도하지 못하게 한다. 넘으면 429를 돌려준다.
			services.AddRateLimiter(options =>
			{
				options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
				options.AddPolicy(AccountRateLimitPolicy, httpContext =>
					RateLimitPartition.GetFixedWindowLimiter(
						httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
						_ => new FixedWindowRateLimiterOptions
						{
							PermitLimit = AccountRequestsPerMinute,
							Window = TimeSpan.FromMinutes(1)
						}));
			});

			services.AddControllers().AddJsonOptions(options =>
			{
				options.JsonSerializerOptions.PropertyNamingPolicy = null;
				options.JsonSerializerOptions.DictionaryKeyPolicy = null;
			});

			services.AddDbContext<AppDbContext>(options =>
				options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

			services.AddDbContext<SharedDbContext>(options =>
				options.UseSqlServer(Configuration.GetConnectionString("SharedConnection")));
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseHttpsRedirection();

			app.UseRouting();

			app.UseRateLimiter();

			app.UseAuthorization();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});
		}
	}
}
