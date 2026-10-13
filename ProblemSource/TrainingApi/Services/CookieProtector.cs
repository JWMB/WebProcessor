using Microsoft.AspNetCore.DataProtection;

namespace TrainingApi.Services
{
	public interface ICookieProtector
	{
		string Protect<T>(T data);
		T? Unprotect<T>(string data);
	}

	public class NullCookieProtector : ICookieProtector
	{
		public string Protect<T>(T data) => System.Text.Json.JsonSerializer.Serialize(data);

		public T? Unprotect<T>(string data) => System.Text.Json.JsonSerializer.Deserialize<T>(data);
	}

	public class CookieProtector : ICookieProtector
	{
		private readonly IDataProtector protector;

		public CookieProtector(IDataProtectionProvider provider)
		{
			protector = provider.CreateProtector("CookieEncryptionPurpose");
		}
		public string Protect<T>(T data)
		{
			var str = System.Text.Json.JsonSerializer.Serialize(data);
			return protector.Protect(str);
		}
		public T? Unprotect<T>(string data)
		{
			var str = protector.Unprotect(data);
			return System.Text.Json.JsonSerializer.Deserialize<T>(str);
		}
	}
}
