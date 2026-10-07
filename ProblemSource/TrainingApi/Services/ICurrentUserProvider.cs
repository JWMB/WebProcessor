using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using ProblemSourceModule.Models;
using ProblemSourceModule.Services.Storage;
using System.Security.Claims;
using TrainingApi.Authorization;

namespace TrainingApi.Services
{
    public interface ICurrentUserProvider
    {
        User? User { get; }
        User UserOrThrow
        {
            get
            {
                var user = User;
                if (user == null)
                    throw new Exception("No user");
                return user;
            }
        }
    }

    public class WebUserProvider : ICurrentUserProvider
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IUserRepository userRepository;

        public WebUserProvider(IHttpContextAccessor httpContextAccessor, IUserRepository userRepository)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.userRepository = userRepository;
        }

        public User? User => GetUserWithImpersonation().Result;

        public async Task<User?> GetUserWithImpersonation()
        {
            var principal = httpContextAccessor.HttpContext?.User;

			var user = await GetUser(userRepository, principal);
            if (user?.Role == Roles.Admin || user?.Role == Roles.SuperAdmin)
            {
                var impersonated = GetRequestImpersonatedUser(httpContextAccessor.HttpContext?.Request);
                if (impersonated != null)
                {
                    var found = await userRepository.Get(impersonated);
                    if (found == null)
                        throw new Exception($"Impersonated not found: '{impersonated}'");
                    return found;
                }
            }
            return user;
        }

        private static string? GetRequestImpersonatedUser(HttpRequest? request)
        {
            if (request == null) return null;

            if (request.Query.TryGetValue("impersonate", out var fromQuery))
            {
                var name = fromQuery.FirstOrDefault();
                if (!string.IsNullOrEmpty(name)) return name;
            }
            if (request.Headers.TryGetValue("Impersonate-User", out var fromHeader))
            {
                var name = fromHeader.FirstOrDefault();
                if (!string.IsNullOrEmpty(name)) return name;
            }
            return null;
        }

        public static string? GetNameClaim(ClaimsPrincipal? principal) => principal?.Claims.Any() == true ? principal?.Claims.First(o => o.Type == ClaimTypes.Name).Value : null;
        public static string? GetClaim(ClaimsPrincipal? principal, string claimType) => principal?.Claims.Any() == true ? principal?.Claims.FirstOrDefault(o => o.Type == claimType)?.Value ?? null : null;

        public static async Task<User?> GetUser(IUserRepository users, ClaimsPrincipal? principal)
        {
            var nameClaim = GetNameClaim(principal);
            var user = nameClaim == null ? null : await users.Get(nameClaim);
            // Debug user
            if (user == null && principal != null)
            {
                if (GetClaim(principal, ClaimTypes.Actor) == "IntegrationTest")
                    return new User { Email = nameClaim!, Role = GetClaim(principal, ClaimTypes.Role) ?? "" };

                if (System.Diagnostics.Debugger.IsAttached && nameClaim == FakeDevUser.Email)
                    return FakeDevUser;
            }
            return user;
        }

        public static User FakeDevUser => new User { Email = "dev", Role = Roles.Admin };

        //private const string MfaClaimName = "mfa";
        //private const string MfaRequireValidationValue = "y";

        public static ClaimsPrincipal CreatePrincipal(User user, bool isIntegrationTestUser = false, string? authenticationType = null, MfaClaimUtls.MfaStatus mfa = MfaClaimUtls.MfaStatus.None)
        {
            // TODO: move
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
				MfaClaimUtls.CreateClaim(mfa),
			};
            if (isIntegrationTestUser)
                claims.Add(new Claim(ClaimTypes.Actor, "IntegrationTest"));

            return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType ?? CookieAuthenticationDefaults.AuthenticationScheme));
        }

        public static async Task Signin(User user, HttpContext context, bool? hasValidatedMfa = null)
        {
            var mfaStatus = user.MfaEnabled != true ? MfaClaimUtls.MfaStatus.None
                : (hasValidatedMfa == true ? MfaClaimUtls.MfaStatus.ValidationPassed : MfaClaimUtls.MfaStatus.ValidationRequired);

			var principal = CreatePrincipal(user, mfa: mfaStatus);
			var authProperties = new AuthenticationProperties
			{
				//AllowRefresh = <bool>, // Refreshing the authentication session should be allowed.
				//ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1), // The time at which the authentication ticket expires. A value set here overrides the ExpireTimeSpan option of CookieAuthenticationOptions set with AddCookie.

				IsPersistent = true,
				// Whether the authentication session is persisted across multiple requests. When used with cookies, controls
				// whether the cookie's lifetime is absolute (matching the lifetime of the authentication ticket) or session-based.

				IssuedUtc = DateTimeOffset.UtcNow, // The time at which the authentication ticket was issued.
												   //RedirectUri = <string> // The full path or absolute URI to be used as an http redirect response value.
			};
			await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

		}
	}
}
