using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using TrainingApi.Services;

namespace TrainingApi
{
	public interface IApiKeyRepository
	{
		Task<ApiKeyUser?> Get(string apiKey);
	}

    public class InMemoryApiKeyRepository : IApiKeyRepository
    {
		//public record Config(IEnumerable<ApiKeyUser> Users);
        private readonly Dictionary<string, ApiKeyUser> users;

        public InMemoryApiKeyRepository(IEnumerable<ApiKeyUser> users) //Config config)
        {
			this.users = users.ToDictionary(o => o.Key); //config.Users
		}
        public Task<ApiKeyUser?> Get(string apiKey) => Task.FromResult(users.GetValueOrDefault(apiKey));
    }

    public class ApiKeyUser
	{
		public required string Key { get; set; }
		public required string UserName { get; set; }
		public string Role { get; set; } = string.Empty;
	}

	public class ApiKeyAuthenticationSchemeOptions : AuthenticationSchemeOptions { }

	public class ApiKeyAuthenticationSchemeHandler : AuthenticationHandler<ApiKeyAuthenticationSchemeOptions>
	{
        private readonly IApiKeyRepository repository;
		public const string SchemeName = "ApiKey";

        public ApiKeyAuthenticationSchemeHandler(IApiKeyRepository repository, IOptionsMonitor<ApiKeyAuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
			: base(options, logger, encoder)
        {
            this.repository = repository;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
		{
			var apiKey = Context.Request.Headers["X-API-KEY"].FirstOrDefault();
			if (string.IsNullOrEmpty(apiKey))
				return AuthenticateResult.Fail("Invalid X-API-KEY");

			var apiUser = await repository.Get(apiKey);
			if (apiUser == null)
				return AuthenticateResult.Fail("Invalid X-API-KEY");
			var user = new ProblemSourceModule.Models.User { Email = apiUser.UserName, Role = apiUser.Role };
			var principal = WebUserProvider.CreatePrincipal(user, false, Scheme.Name);
			var ticket = new AuthenticationTicket(principal, Scheme.Name);
			return AuthenticateResult.Success(ticket);
		}
	}
}
