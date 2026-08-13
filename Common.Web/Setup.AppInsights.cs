using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Web
{
	public class SetupAppInsights
	{
		public static void Add(IServiceCollection services, ConfigurationManager configurationManager)
		{
			//services.Configure<TelemetryConfiguration>(telemetryConfiguration =>
			//{
			//	var builder = telemetryConfiguration.DefaultTelemetrySink.TelemetryProcessorChainBuilder;
			//	telemetryConfiguration.DefaultTelemetrySink.TelemetryProcessorChainBuilder
			//		.UseAdaptiveSampling(maxTelemetryItemsPerSecond: 5, excludedTypes: "Trace;Request;Exception");
			//});
			//services.AddApplicationInsightsTelemetry(new ApplicationInsightsServiceOptions
			//{
			//	EnableAdaptiveSampling = false,
			//});

			//services.AddSingleton<ITelemetryInitializer, UserInformationTelemetryInitializer>();
		}

		public static void Configure(IApplicationBuilder app, IConfiguration config, bool isDevelopment)
		{
			var aiConn = config.GetValue("ApplicationInsights:ConnectionString", "");
			if (aiConn == "SECRET" || aiConn == string.Empty)
			{
				if (isDevelopment == false)
					Console.WriteLine($"Warning: InstrumentationKey not set ({aiConn})");
				//throw new ArgumentException("InstrumentationKey not set");
			}
			else
			{
				var telemetryConfig = app.ApplicationServices.GetService<TelemetryConfiguration>();
				if (telemetryConfig != null)
				{
					telemetryConfig.ConnectionString = aiConn;
					var telemetry = new TelemetryClient(telemetryConfig);
					telemetry.TrackEvent("Application start");
					telemetry.TrackTrace("Trace Application start");
				}
			}
		}
	}
}
