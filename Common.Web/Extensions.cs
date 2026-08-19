using Microsoft.Extensions.Hosting;

namespace Common.Web
{
	public static class Extensions
	{
		public static bool HasEnvironmentPart(this IHostEnvironment hostEnvironment, string environmentPart)
		{
			ArgumentNullException.ThrowIfNull(hostEnvironment);
			return hostEnvironment.EnvironmentName.Split('.')
				.Any(part => string.Equals(environmentPart, part, StringComparison.OrdinalIgnoreCase));
		}
		public static bool HasDevelopmentEnvironment(this IHostEnvironment hostEnvironment)
			=> hostEnvironment.HasEnvironmentPart(Environments.Development);
	}
}
