using Microsoft.AspNetCore.Authentication.Cookies;
using ProblemSourceModule.Models;
using ProblemSourceModule.Services.Storage;
using System.Security.Claims;

namespace TrainingApi.Services
{
    public interface IAccessResolver
    {
        bool HasAccess(int trainingId, AccessLevel level);
        bool HasAccess(User user, int trainingId, AccessLevel level);
    }
    public enum AccessLevel
    {
        None = 0,
        Read = 1,
        Write = 2
    }

    public class AccessResolver : IAccessResolver
    {
        private readonly IUserProvider userProvider;

        public AccessResolver(IUserProvider userProvider)
        {
            this.userProvider = userProvider;
        }

        public bool HasAccess(User user, int trainingId, AccessLevel level) => user.Trainings.Any(o => o.Value.Contains(trainingId));

        public bool HasAccess(int trainingId, AccessLevel level)
        {
            var user = userProvider.User;
            return user == null ? false : HasAccess(user, trainingId, level);
        }
    }


    public interface IUserProvider
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

    public class WebUserProvider : IUserProvider
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IUserRepository userRepository;

        public WebUserProvider(IHttpContextAccessor httpContextAccessor, IUserRepository userRepository)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.userRepository = userRepository;
        }

        public User? User
        {
            get
            {
                var nameClaim = httpContextAccessor.HttpContext?.User.Claims.First(o => o.Type == ClaimTypes.Name).Value;
                if (nameClaim == null) return null;
                var user = userRepository.Get(nameClaim).Result;
                //if (user == null && fallbackToDev && System.Diagnostics.Debugger.IsAttached && nameClaim == "dev")
                //    return new User { Role = Roles.Admin };
                return user;
            }
        }

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
