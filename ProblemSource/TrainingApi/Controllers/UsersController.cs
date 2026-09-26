using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ProblemSourceModule.Services.Storage;
using TrainingApi.Services;
using ProblemSourceModule.Models;
using System.ComponentModel.DataAnnotations;
using TrainingApi.ErrorHandling;
using ProblemSourceModule.Services;

namespace TrainingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository userRepository;
        private readonly IAuthenticateUserService authenticateUserService;
        private readonly ICurrentUserProvider userProvider;
        private readonly CreateUserWithTrainings createUserWithTrainings;
        private readonly ITrainingRepository trainingRepository;
		private readonly IMfaService mfaService;
		private readonly ICookieProtector cookieProtector;
		private readonly ILogger<UsersController> log;

        public UsersController(IUserRepository userRepository, IAuthenticateUserService authenticateUserService, ICurrentUserProvider userProvider, 
            CreateUserWithTrainings createUserWithTrainings, ITrainingRepository trainingRepository, IMfaService mfaService, ICookieProtector cookieProtector, ILogger<UsersController> logger)
        {
            this.userRepository = userRepository;
            this.authenticateUserService = authenticateUserService;
            this.userProvider = userProvider;
            this.createUserWithTrainings = createUserWithTrainings;
            this.trainingRepository = trainingRepository;
			this.mfaService = mfaService;
			this.cookieProtector = cookieProtector;
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
		[HttpPost("createusers")]
		public async Task<CreateUsersResponseDto> PostCreateUsers([FromBody] CreateUsersRequestDto dto)
		{
            //await fetch("https://localhost:7174/api/Users", {
            //    "credentials": "include", "mode": "cors",
            //    "headers": { "Accept": "application/json", "Content-Type": "application/json" },
            //    "method": "POST",
            //    "body": '{ "usernames": ["alexandra.englundnilsson@edu.habo.se"] }'
            //});

			var emailsAndPassword = new List<CreatedUserInfo>();
			var rnd = new Random();
			foreach (var email in dto.Emails)
			{
                if (email?.Any() != true)
                {
                    Console.WriteLine("Missing email");
                    continue;
                }
				var usr = await userRepository.Get(email);
				emailsAndPassword.Add(new(email, usr == null ? CreateUserWithTrainings.CreatePassword(rnd) : ""));
			}

			foreach (var item in emailsAndPassword.Where(o => o.Password.Any()))
			{
				var usr = new User { Email = item.Email, Role = "Teacher", PasswordForHashing = item.Password, Trainings = new(), MfaEnabled = true };
				try
				{
					await userRepository.Add(usr);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error creating user: {item.Email} ({item.Password})");
				}
			}
			return new CreateUsersResponseDto(emailsAndPassword);
		}

		public record CreateUsersRequestDto(List<string> Emails);
		public record CreateUsersResponseDto(List<CreatedUserInfo> Emails);
		public record CreatedUserInfo(string Email, string Password);

		[Authorize(Policy = RolesRequirement.Admin)]
        [HttpPost]
        public async Task<List<CreatedUserInfo>> Post([FromBody] CreateUserDto dto)
        {
            if (dto.Usernames?.Any() == true)
            {
                return (await PostCreateUsers(new CreateUsersRequestDto(dto.Usernames))).Emails;
			}
            // TODO: use regular model validation
            if (string.IsNullOrEmpty(dto.Username))
                throw new ArgumentNullException(nameof(dto.Username));
            if (string.IsNullOrEmpty(dto.Password))
                throw new ArgumentNullException(nameof(dto.Password));

            dto.Normalize();

            await userRepository.Add(new User
            {
                Email = dto.Username,
                Role = dto.Role,
                Trainings = new(),
                PasswordForHashing = dto.Password
            });

            return new List<CreatedUserInfo> { new CreatedUserInfo(dto.Username, dto.Password) };
        }

		[Authorize(Policy = RolesRequirement.Admin, AuthenticationSchemes = $"{ApiKeyAuthenticationSchemeHandler.SchemeName},{CookieAuthenticationDefaults.AuthenticationScheme}")]
		[HttpGet("trainingUsername/{id}")]
		public async Task<IActionResult> GetTrainingUsername(int id)
        {
			var role = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.Role) : null;
			if (role != Roles.Admin)
				return new ForbidResult();
            var training = await trainingRepository.Get(id);
			return Ok(new { Username = training?.Username ?? "", Id = id });
        }

		[Authorize(Policy = RolesRequirement.Admin, AuthenticationSchemes = $"{ApiKeyAuthenticationSchemeHandler.SchemeName},{CookieAuthenticationDefaults.AuthenticationScheme}")]
		[HttpPost("getOrCreate")]
        public async Task<ActionResult<GetUserDto>> GetOrCreateFromApp([FromQuery] string username)
        {
			if (string.IsNullOrEmpty(username))
				return new BadRequestResult();

			if (User?.Identity?.IsAuthenticated != true)
                return new ForbidResult();
            //var callingUsername = User.FindFirstValue(ClaimTypes.Name);
            //if (callingUsername == null)
            //    return new ForbidResult();
            var role = User.FindFirstValue(ClaimTypes.Role);
            if (role != Roles.Admin)
				return new ForbidResult();

            var user = await userRepository.Get(username);
			if (user == null)
            {
                var trainingPlan = "chatclientplan";
				var createdResult = await createUserWithTrainings.CreateUser(username, new Dictionary<string, int>
                {
                    ["self"] = 1
                }, trainingPlan, new ProblemSource.Models.TrainingSettings { }, actuallyCreate: true);
                user = createdResult.User;
			}
			return new GetUserDto { Username = user.Email, Trainings = user.Trainings, Role = user.Role };
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
            if (dto.Password != null) user.PasswordForHashing = dto.Password;
            if (dto.Trainings != null) user.Trainings = new UserTrainingsCollection(dto.Trainings);

            await userRepository.Update(user);
            return Ok();
        }

        [Authorize]
        [HttpGet]
        [Route("GetLoggedInUser")]
        public GetUserDto GetLoggedInUser() => GetUserDto.FromUser(userProvider.UserOrThrow);

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
            //TODO: move to e.g. B2C
            var user = await authenticateUserService.GetUser(credentials.Username, credentials.Password);
            if (user == null)
            {
                log.LogWarning($"Login failed for '{credentials.Username}'. Exists:{(await userRepository.Get(credentials.Username)) != null}");
                return Unauthorized(new { Title = $"Login failed - please check your spelling" });
            }

            if (user.Role == "Admin")
            {
                user.MfaEnabled = true;
            }
            var principal = WebUserProvider.CreatePrincipal(user, requireMfa: user.MfaEnabled == true);
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

            var skipMfa = System.Diagnostics.Debugger.IsAttached;

			return Ok(new LoginResultDto(user.Role, 
                MfaMustValidate: skipMfa != true && user.MfaEnabled == true,
                MfaMustRegister: skipMfa != true && (user.MfaEnabled == true && user.MfaSecretKey?.Any() != true)));
        }

		private const int NumTotpDigits = 6;

		public class MfaLoginDto
		{
			public required string Email { get; set; }
			public required string Code { get; set; }
			public string? ReturnUrl { get; set; }
		}

        [HttpPost("mfa-verify")]
        public async Task<bool> PostMfaVerify(MfaLoginDto dto)
        {
            return await mfaService.VerifyTwoFactorAuthentication(dto.Email, dto.Code, NumTotpDigits); // request.NumTotpDigits
        }

        public record MfaVerifyGetDto(int NumTotpDigits, string Issuer);
        [HttpGet("mfa-verify")]
        public async Task<MfaVerifyGetDto> GetMfaVerify() => new MfaVerifyGetDto(NumTotpDigits, mfaService.Issuer);


        [Authorize]
		[HttpPost("mfa-enable")]
		public async Task<bool> PostMfaEnable(MfaLoginDto dto)
        {
            if (dto.Email?.Any() != true)
                return false;
            var cookie = Request.Cookies[MfaCookie.DefaultName];
            if (cookie == null)
                return false;
            var mfaCookie = cookieProtector.Unprotect<MfaCookie>(cookie);

			//var login = helper.GetLoginFromEmail(request.Email);
			//if (login == null)
			//	throw new BadRequestException($"Login not found {request.Email}");

			if (mfaCookie?.Secret?.Any() != true)
				throw new Exception($"Login not configured for MFA {dto.Email}");

            var result = await mfaService.Enable(dto.Email, mfaCookie.Secret, dto.Code, NumTotpDigits);
            //if (result)
            //    await mfaService.Enable(dto.Email, mfaCookie.Secret, dto.Code, NumTotpDigits);
			return result;
        }

        public record MfaCookie(string Secret)
        {
            public const string DefaultName = "card";
        }

		public record MfaEnableDto(string AuthenticatorUri, int NumTotpDigits, string Issuer);
        [HttpGet("mfa-enable")]
		public async Task<MfaEnableDto> GetMfaEnable(string email)
        {
			(string secretKey, string qrCodeUrl) = await mfaService.GenerateTwoFactorInfo(email);

            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddMinutes(5)
            };
            var cookie = new MfaCookie(secretKey);
			Response.Cookies.Append(MfaCookie.DefaultName, cookieProtector.Protect(cookie), options);

			return new MfaEnableDto(qrCodeUrl, NumTotpDigits, mfaService.Issuer);
		}

        [HttpPut]
        [Route("movetrainings")]
        public async Task MoveTrainings([FromBody] MoveTrainingsDto input)
        {
            var user = userProvider.UserOrThrow;

            if (!user.Trainings.TryGetValue(input.FromGroup, out var idsFromGroup))
                throw new HttpException($"Group not found: {input.FromGroup}", StatusCodes.Status400BadRequest);

            if (!user.Trainings.TryGetValue(input.ToGroup, out var idsToGroup))
                throw new HttpException($"Group not found: {input.ToGroup}", StatusCodes.Status400BadRequest);

            if (idsFromGroup.Intersect(input.TrainingIds).Count() != input.TrainingIds.Count())
                throw new HttpException($"Id mismatch", StatusCodes.Status400BadRequest);

            user.Trainings[input.FromGroup] = idsFromGroup.Except(input.TrainingIds).ToList();
            user.Trainings[input.ToGroup] = idsToGroup.Concat(input.TrainingIds).Distinct().ToList();

            await userRepository.Update(user);
        }

        public class MoveTrainingsDto
        {
            public List<int> TrainingIds { get; set; } = new();
            public string FromGroup { get; set; } = string.Empty;
            public string ToGroup { get; set; } = string.Empty;
        }

	public readonly record struct LoginResultDto(string Role, bool MfaMustValidate = false, bool MfaMustRegister = false);
    }

    public class LoginCredentials
    {
        [Required]
        [StringLength(50, MinimumLength = 5)]
        public string Username { get; set; } = string.Empty;
        [Required]
        [StringLength(14, MinimumLength = 5)]
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

    public class CreateUserDto //: GetUserDto
    {
        public List<string>? Usernames { get; set; } // Stupid, something with nginx routing catches Users/createusers and returns 405? AHA no, it was rsync copying old files....
        public string Username { get; set; } = "";
		public string Role { get; set; } = "";
		public string Password { get; set; } = "";
        public void Normalize()
        {
            Password = Password.Trim();
            Username = Username.Trim();
        }
    }

    public class PatchUserDto
    {
        public string? Role { get; set; }
        public string? Password { get; set; }
        public Dictionary<string, List<int>>? Trainings { get; set; }
    }
}