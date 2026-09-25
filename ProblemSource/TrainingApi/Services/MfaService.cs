using OtpNet;
using ProblemSourceModule.Services.Storage;
using System.Security.Cryptography;

namespace TrainingApi.Services
{
	public interface ILoginMfaHandler
	{
		Task<LoginMfaSettings?> GetMfaSettings(string userId);
		Task SaveMfaSettings(LoginMfaSettings settings);
	}

	public class MongoLoginMfaHandler : ILoginMfaHandler
	{
		private readonly IUserRepository userRepository;

		public MongoLoginMfaHandler(IUserRepository userRepository)
		{
			this.userRepository = userRepository;
		}
		public async Task<LoginMfaSettings?> GetMfaSettings(string userId)
		{
			var user = await userRepository.Get(userId);
			if (user == null)
				return null;
			return new LoginMfaSettings { UserId = userId, MfaEnabled = user.MfaEnabled, MfaSecretKey = user.MfaSecretKey };
		}

		public async Task SaveMfaSettings(LoginMfaSettings settings)
		{
			var user = await userRepository.Get(settings.UserId);
			if (user == null)
				return;
			user.MfaEnabled = settings.MfaEnabled;
			user.MfaSecretKey = settings.MfaSecretKey;
			await userRepository.Upsert(user);
		}
	}

	public class NullLoginMfaHandler : ILoginMfaHandler
	{
		public async Task<LoginMfaSettings?> GetMfaSettings(string userId)
		{
			return new LoginMfaSettings { UserId = userId };
		}
		public async Task SaveMfaSettings(LoginMfaSettings settings)
		{ }
	}

	public class LoginMfaSettings
	{
		public required string UserId { get; set; }
		public string? MfaSecretKey { get; set; }
		public bool? MfaEnabled { get; set; }
	}

	public interface IMfaService
	{
		Task<(string secretKey, string qrCodeUrl)> GenerateTwoFactorInfo(string username);
		Task<bool> Enable(string email, string secretKey, string code, int numDigits);
		Task<bool> Disable(string email);
		Task<bool> VerifyTwoFactorAuthentication(string email, string secretKey, string code, int numDigits);
	}

	public class MfaService : IMfaService
	{
		private readonly string issuer = "CurricuLLM";
		private readonly ILoginMfaHandler loginMfaHandler;

		public string Issuer => issuer;

		public MfaService(ILoginMfaHandler loginMfaHandler)
		{
			this.loginMfaHandler = loginMfaHandler;
		}

		private string GenerateRandomString(int length)
		{
			const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
			var randomBytes = new byte[length];
			using var rng = RandomNumberGenerator.Create();
			// Fill the array with random bytes
			rng.GetBytes(randomBytes);
			//Get the corresponding character for each the byte using modulus
			return new string(randomBytes.Select(x => validChars[x % validChars.Length]).ToArray());
		}

		public async Task<(string secretKey, string qrCodeUrl)> GenerateTwoFactorInfo(string username)
		{
			var secretKey = GenerateRandomString(16);
			var encodedUsername = Uri.EscapeDataString(username);
			var qrCodeUrl = $"otpauth://totp/{encodedUsername}?secret={secretKey}&issuer={issuer}";

			return (secretKey, qrCodeUrl);
		}

		private async Task<LoginMfaSettings?> GetLogin(string email)
		{
			return await loginMfaHandler.GetMfaSettings(email);
		}

		private async Task SaveLogin(LoginMfaSettings login, string? secretKey)
		{
			login.MfaSecretKey = secretKey;
			login.MfaEnabled = secretKey != null;
			await loginMfaHandler.SaveMfaSettings(login);
		}

		public async Task<bool> Enable(string email, string secretKey, string code, int numDigits)
		{
			var login = await GetLogin(email);
			if (login == null || Verify(secretKey, code, numDigits) == false)
				return false;
			await SaveLogin(login, secretKey);
			return true;
		}

		public async Task<bool> Disable(string email)
		{
			var login = await GetLogin(email);
			if (login == null)
				return false;
			await SaveLogin(login, null);
			return true;
		}

		public async Task<bool> VerifyTwoFactorAuthentication(string email, string secretKey, string code, int numDigits)
			=> await GetLogin(email) != null && Verify(secretKey, code, numDigits);

		private bool Verify(string secretKey, string code, int numDigits)
		{
			if (string.IsNullOrEmpty(secretKey) && string.IsNullOrEmpty(code))
				return false;

			try
			{
				var totp = new Totp(Base32Encoding.ToBytes(secretKey), totpSize: numDigits);
				var verifed = totp.VerifyTotp(code, out _, new VerificationWindow(2, 2)); // Adjust window size as needed
				return verifed;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error verifying Totp: {ex.Message}");
				return false;
			}
		}
	}
}
