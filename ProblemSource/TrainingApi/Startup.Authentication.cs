using Common.Web;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using System.Text;
using TrainingApi.Authorization;
using TrainingApi.Services;

namespace TrainingApi
{
	public class StartupAuthentication
	{
		public void ConfigureServices(IServiceCollection services, ConfigurationManager configurationManager, IHostEnvironment env)
		{
			services.AddScoped<IUserLoginService, UserLoginService>();
			services.AddTransient<ICurrentUserProvider, WebUserProvider>();
			services.AddTransient<ITrainingAccessResolver, TrainingAccessResolver>();

			services.AddSingleton<IAuthorizationHandler, AtLeastRoleAuthorizationHandler>();

			services.AddSingleton<ILoginMfaHandler, MongoLoginMfaHandler>(); // MongoLoginMfaHandler NullLoginMfaHandler
			services.AddSingleton<IMfaService, MfaService>();

			var apiKeyUsers = new List<ApiKeyUser>();
			configurationManager.GetSection("AppSettings:ApiKeyUsers").Bind(apiKeyUsers);
			services.AddSingleton<IApiKeyRepository>(sp => new InMemoryApiKeyRepository(apiKeyUsers));

			services.AddAuthorization();

			ConfigureAuthentication(services, configurationManager, env);
			//services.AddAuthorization(options =>
			//{
			//	//options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicy(
			//	//    );
			//	options.AddPolicy(RolesRequirement.Admin, policy =>
			//	{
			//		policy.RequireAuthenticatedUser();
			//		policy.RequireAssertion(ctx => {
			//			return ctx.User.AtLeastRole(RolesRequirement.Admin);
			//		});
			//		policy.Requirements.Add(new RolesRequirement(RolesRequirement.Admin));
			//	});
			//	options.AddPolicy(RolesRequirement.AdminOrTeacher, policy => policy.Requirements.Add(new RolesRequirement(RolesRequirement.AdminOrTeacher)));
			//});
		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.HasDevelopmentEnvironment())
			{
				app.Use(async (context, next) =>
				{
					if (context.User?.Claims.Any() == false) // System.Diagnostics.Debugger.IsAttached
					{
						// For the lazy developer - swagger and test client are automatically authenticated
						var referer = context.Request.GetTypedHeaders().Referer;
						// when from swagger and localhost
						var autologin = referer?.AbsolutePath.Contains("/swagger/") == true
							|| referer?.AbsoluteUri.StartsWith("http://localhost:") == true
							|| referer == null;

						Console.WriteLine($"referer={referer}, autologin={autologin}");

						if (!autologin)
						{
							// when connecting for websockets (context.WebSockets.IsWebSocketRequest is false when connecting)
							if (context.Request.Method == "CONNECT" && context.Request.Path == "/realtime")
								autologin = true;
						}

						if (autologin && context.Request.Cookies.Any(o => o.Key == "autologin" && o.Value == "0"))
							autologin = false;

						if (autologin)
							context.User = WebUserProvider.CreatePrincipal(WebUserProvider.FakeDevUser);
					}

					await next.Invoke();
				});
			}
		}

		private void ConfigureAuthentication(IServiceCollection services, IConfiguration config, IHostEnvironment env)
		{
			var combinedScheme = "JWT_OR_COOKIE";

			services.AddAuthentication(options =>
			{
				// https://weblog.west-wind.com/posts/2022/Mar/29/Combining-Bearer-Token-and-Cookie-Auth-in-ASPNET
				options.DefaultScheme = combinedScheme;
				options.DefaultChallengeScheme = combinedScheme;
				options.DefaultAuthenticateScheme = combinedScheme;
				options.DefaultForbidScheme = combinedScheme;

				//var authSchemeBuilderMars = new AuthenticationSchemeBuilder(ApiKeyAuthenticationSchemeHandler.SchemeName);
				//authSchemeBuilderMars.HandlerType = typeof(ApiKeyAuthenticationSchemeHandler);
				//            // Override already registered schemas
				//            var existing = options.Schemes.SingleOrDefault(s => s.Name == authSchemeBuilderMars.Name);
				//if (existing != null)
				//	existing.HandlerType = authSchemeBuilderMars.HandlerType;
				//options.SchemeMap[authSchemeBuilderMars.Name] = authSchemeBuilderMars;

			}).AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationSchemeHandler>(ApiKeyAuthenticationSchemeHandler.SchemeName, options =>
			{
			}).AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
			{
				options.ExpireTimeSpan = TimeSpan.FromDays(1);

				//options.Cookie.SecurePolicy = true ? CookieSecurePolicy.None : CookieSecurePolicy.Always; //_environment.IsDevelopment()
				options.Cookie.SameSite = env.IsDevelopment() ? Microsoft.AspNetCore.Http.SameSiteMode.None : Microsoft.AspNetCore.Http.SameSiteMode.Lax;
				if (Enum.TryParse<Microsoft.AspNetCore.Http.SameSiteMode>(config.GetValue("Cookies:SameSite", ""), out var sameSiteConfig))
					options.Cookie.SameSite = sameSiteConfig;

				options.Events = new CustomCookieAuthEvents();

			}).AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
			{
				// This is a way to invalidate older tokens in case of exposure
				var issuedAfter = DateTime.Parse(config["Token:IssuedAfter"] ?? "", System.Globalization.CultureInfo.InvariantCulture);
				var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Token:TokenSigningKey"] ?? ""));

				options.TokenValidationParameters = new TokenValidationParameters
				{
					ValidIssuer = config["Token:ValidIssuer"],
					ValidateIssuer = true,

					ValidAudiences = config["Token:ValidAudiences"]?.Split(',') ?? new[] { "" },
					ValidateAudience = true,

					ValidateIssuerSigningKey = true,
					IssuerSigningKey = securityKey,

					ValidateLifetime = true,
					LifetimeValidator = (_, _, securityToken, validationParameters) =>
						securityToken.ValidFrom > issuedAfter &&
						securityToken.ValidTo > DateTime.UtcNow
				};
				options.Validate();

			}).AddPolicyScheme(combinedScheme, combinedScheme, options =>
			{
				// runs on each request
				options.ForwardDefaultSelector = context =>
				{
					// filter by auth type
					var authorization = context.Request.Headers[HeaderNames.Authorization];
					if (authorization.ToString().StartsWith(JwtBearerDefaults.AuthenticationScheme) == true)
						return JwtBearerDefaults.AuthenticationScheme;

					// otherwise always check for cookie auth
					return CookieAuthenticationDefaults.AuthenticationScheme;
				};
			});
		}
	}
}
