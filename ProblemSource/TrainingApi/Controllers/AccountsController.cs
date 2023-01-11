using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ProblemSourceModule.Services.Storage;
using TrainingApi.Services;
using ProblemSourceModule.Models;

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

        [Authorize(Policy = RolesRequirement.Admin)]
        [HttpGet]
        public async Task<IEnumerable<GetUserDto>> GetAll()
        {
            return (await userRepository.GetAll()).Select(GetUserDto.FromUser);
        }

        [Authorize(Policy = RolesRequirement.AdminOrTeacher)]
        [HttpGet]
        [Route("GetOne")] // TODO: For some reason, we need an explicit path for unit/integration tests
        [Route("{id}")]
        public async Task<ActionResult<GetUserDto>> Get([FromQuery]string id)
        {
            if (User.FindFirstValue(ClaimTypes.Role) != Roles.Admin)
            {
                if (User.FindFirstValue(ClaimTypes.Name) != id)
                    return Forbid();
            }

            var user = await userRepository.Get(id);
            if (user == null)
                return NotFound();
            return Ok(GetUserDto.FromUser(user));
        }

        [Authorize(Policy = RolesRequirement.Admin)]
        [HttpPost]
        public async Task Post([FromBody] CreateUserDto dto)
        {
            // TODO: use regular model validation
            if (string.IsNullOrEmpty(dto.Username))
                throw new ArgumentNullException(nameof(dto.Username));
            if (string.IsNullOrEmpty(dto.Password))
                throw new ArgumentNullException(nameof(dto.Password));

            await userRepository.Add(new User
            {
                Email = dto.Username,
                Role = dto.Role,
                Trainings = new(),
                HashedPassword = ProblemSourceModule.Models.User.HashPassword(dto.Username, dto.Password)
            });
        }

        [Authorize(Policy = RolesRequirement.Admin)]
        [HttpPatch]
        [Route("id")]
        public async Task<ActionResult> Patch([FromQuery] string id, [FromBody] PatchUserDto dto)
        {
            var user = await userRepository.Get(id);
            if (user == null)
                return NotFound();
            if (dto.Role != null) user.Role = dto.Role;
            if (dto.Password != null) user.HashedPassword = ProblemSourceModule.Models.User.HashPassword(id, dto.Password);
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
        public async Task<ActionResult<LoginResultDto>> Login([FromBody] LoginCredentials credentials)
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
            
            return Ok(new LoginResultDto(user.Role));
        }

        public readonly record struct LoginResultDto(string Role);

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