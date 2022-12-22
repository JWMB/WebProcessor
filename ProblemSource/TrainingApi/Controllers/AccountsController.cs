using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace TrainingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly ILogger<AccountsController> log;
        private readonly Dictionary<string, string> users;

        public AccountsController(IConfiguration configuration, ILogger<AccountsController> logger)
        {
            log = logger;

            var section = configuration.GetSection("Users");
            users = section?.GetChildren().ToDictionary(o => o.Key, o => o.Value ?? "") ?? new();
        }

        [Authorize]
        [HttpGet]
        public async Task<string> Get()
        {
            await Task.Delay(1);
            return "Hello!";
        }

        [HttpPost]
        [Route("login")]
        public async Task<ActionResult> Login([FromBody] LoginCredentials credentials)
        {
            // TODO: just until real auth is implemented
            if (System.Diagnostics.Debugger.IsAttached)
            {
                credentials = new LoginCredentials { Username = "dev user" };
            }
            else if (!users.TryGetValue(credentials.Username, out var storedPwd) || storedPwd != credentials.Password)
            {
                return Unauthorized();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, credentials.Username),
                new Claim(ClaimTypes.Role, Roles.Admin)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

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

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            return Ok("logggg");
        }
    }

    public class LoginCredentials
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}