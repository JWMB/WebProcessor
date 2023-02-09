using Microsoft.AspNetCore.Authentication.Cookies;
using ProblemSourceModule.Models;
using ProblemSourceModule.Services.Storage;
using System.Security.Claims;

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
            var user = await GetUser(userRepository, httpContextAccessor.HttpContext?.User);
            if (user?.Role == Roles.Admin)
            {
                if (httpContextAccessor.HttpContext?.Request.Headers.TryGetValue("Impersonate-User", out var impersonated) == true)
                {
                    var name = impersonated.FirstOrDefault();
                    if (!string.IsNullOrEmpty(name))
                        return await userRepository.Get(name);
                }
            }
            return user;
        }

        public static string? GetNameClaim(ClaimsPrincipal? principal) => principal?.Claims.Any() == true ? principal?.Claims.First(o => o.Type == ClaimTypes.Name).Value : null;

        public static async Task<User?> GetUser(IUserRepository users, ClaimsPrincipal? principal)
        {
            var nameClaim = GetNameClaim(principal);
            var user = nameClaim == null ? null : await users.Get(nameClaim);
            // Debug user
            if (user == null && System.Diagnostics.Debugger.IsAttached)
            {
                if (GetNameClaim(principal) == FakeDevUser.Email)
                    return FakeDevUser;
            }
            return user;
        }

        public static User FakeDevUser => new User { Email = "dev", Role = Roles.Admin };

        public static ClaimsPrincipal CreatePrincipal(User user)
        {
            // TODO: move
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };
            return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        }
    }
}
