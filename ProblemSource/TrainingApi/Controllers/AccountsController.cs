using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ProblemSourceModule.Services.Storage;
using TrainingApi.Services;

namespace TrainingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly IUserRepository userRepository;
        private readonly IAuthenticateUserService authenticateUserService;
        private readonly ILogger<AccountsController> log;

        public AccountsController(IUserRepository userRepository, IAuthenticateUserService authenticateUserService, ILogger<AccountsController> logger)
        {
            this.userRepository = userRepository;
            this.authenticateUserService = authenticateUserService;
            log = logger;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<string> Get()
        {
            await Task.Delay(1);
            return "Hello!";
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task Post()
        {
            await userRepository.Add(new ProblemSourceModule.Services.Storage.User
            {
                Email = "",
                Role = Roles.Admin,
                Trainings = new List<int>(),
                HashedPassword = ProblemSourceModule.Services.Storage.User.HashPassword("pwd")
            });
        }

        [HttpPost]
        [Route("login")]
        public async Task<ActionResult> Login([FromBody] LoginCredentials credentials)
        {
            var user = await authenticateUserService.GetUser(credentials.Username, credentials.Password);
            if (user == null)
            {
                return Unauthorized();
            }

            var principal = CreatePrincipal(user);
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

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
            
            return Ok();
        }

        public static ClaimsPrincipal CreatePrincipal(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };
            return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        }
    }

    public class LoginCredentials
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}