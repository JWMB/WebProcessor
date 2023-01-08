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
        public async Task<IEnumerable<GetUserDto>> GetAll()
        {
            return (await userRepository.GetAll()).Select(GetUserDto.FromUser);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        [Route("id")]
        public async Task<ActionResult<GetUserDto>> Get(string id)
        {
            var user = await userRepository.Get(id);
            if (user == null)
                return NotFound();
            return Ok(GetUserDto.FromUser(user));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task Post([FromBody] CreateUserDto dto)
        {
            await userRepository.Add(new User
            {
                Email = dto.Username,
                Role = dto.Role,
                Trainings = new(),
                HashedPassword = ProblemSourceModule.Services.Storage.User.HashPassword(dto.Username, dto.Password)
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch]
        [Route("id")]
        public async Task<ActionResult> Patch([FromQuery] string id, [FromBody] PatchUserDto dto)
        {
            var user = await userRepository.Get(id);
            if (user == null)
                return NotFound();
            if (dto.Role != null) user.Role = dto.Role;
            if (dto.Password != null) user.HashedPassword = ProblemSourceModule.Services.Storage.User.HashPassword(id, dto.Password);
            if (dto.Trainings != null) user.Trainings = dto.Trainings;

            await userRepository.Update(user);
            return Ok();
        }

        [HttpPost]
        [Route("logout")]
        public async Task Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
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

    public class GetUserDto
    {
        public string Username { get; set; } = string.Empty;

        public string Role { get; set; } = "";
        public Dictionary<string, List<int>> Trainings { get; set; } = new();

        public static GetUserDto FromUser(User user)
        {
            return new GetUserDto { Role = user.Role, Username = user.Email, Trainings = user.Trainings };
        }
    }

    public class CreateUserDto : GetUserDto
    {
        public string Password { get; set; } = "";
    }
    public class PatchUserDto
    {
        public string? Role { get; set; }
        public string? Password { get; set; }
        public Dictionary<string, List<int>>? Trainings { get; set; }
    }
}