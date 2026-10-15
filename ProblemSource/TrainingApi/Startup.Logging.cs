using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Extensibility;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ProblemSourceModule.Services;

namespace TrainingApi
{
	public class StartupLogging
	{
		private readonly ConfigurationManager configuration;
		private List<IConfigureApp> configureAppList = new();

		public StartupLogging(ConfigurationManager configuration)
		{
			this.configuration = configuration;
		}
		public void ConfigureServices(IServiceCollection services, IHostEnvironment env)
		{
			//AddAppInsights(services);

			services.AddSingleton<IStreamingStatistics>(sp => new StreamingStatistics(2000));

			var prometheus = new PrometheusExporter();
			var console = new ConsoleExporter();

			IEnumerable < ITelemetryMetricsExporter> metricsExporters =
				[prometheus];
			IEnumerable<ITelemetryTraceExporter> traceExporters = 
				[];
			configureAppList = 
				[prometheus];

			var builder = services.AddOpenTelemetry()
				.ConfigureResource(resource => resource.AddService("MyWebApi"));

			builder = builder.WithTracing(tracing =>
			{
				tracing
					.AddAspNetCoreInstrumentation()
					.AddHttpClientInstrumentation()
					.AddSource("MyWebApi")
					.SetSampler(new AlwaysOnSampler());

				foreach (var item in traceExporters)
					item.Add(tracing);
			});

			builder = builder.WithMetrics(metrics =>
			{
				metrics
					.AddAspNetCoreInstrumentation()
					.AddRuntimeInstrumentation()
					.AddProcessInstrumentation();

				foreach (var item in metricsExporters)
					item.Add(metrics);
			});
		}

		public void Configure(IApplicationBuilder app)
		{
			foreach (var item in configureAppList)
				item.Configure(app);
		}

		private void AddAppInsights(IServiceCollection services)
		{
			services.AddLogging(builder =>
			{
				builder.AddApplicationInsights(); // AddAzureWebAppDiagnostics
			});

			services.Configure<TelemetryConfiguration>(telemetryConfiguration =>
			{
				var builder = telemetryConfiguration.DefaultTelemetrySink.TelemetryProcessorChainBuilder;
				telemetryConfiguration.DefaultTelemetrySink.TelemetryProcessorChainBuilder
					.UseAdaptiveSampling(maxTelemetryItemsPerSecond: 5, excludedTypes: "Trace;Request;Exception");
			});
			services.AddApplicationInsightsTelemetry(new ApplicationInsightsServiceOptions
			{
				EnableAdaptiveSampling = false,
			});
			services.AddSingleton<ITelemetryInitializer, UserInformationTelemetryInitializer>();
		}

		private void ConfigureApplicationInsights(IApplicationBuilder app)
		{
			bool isDevelopment = true;
			var aiConn = configuration.GetValue("ApplicationInsights:ConnectionString", "");
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
					var telemetry = new Microsoft.ApplicationInsights.TelemetryClient(telemetryConfig);
					telemetry.TrackEvent("Application start");
					telemetry.TrackTrace("Trace Application start");
				}
			}
		}
	}

	public interface ITelemetryTraceExporter
	{
		void Add(TracerProviderBuilder builder);
	}
	public interface ITelemetryMetricsExporter
	{
		void Add(MeterProviderBuilder builder);
	}
	public interface IConfigureApp
	{
		void Configure(IApplicationBuilder app);
	}

	public class ConsoleExporter : ITelemetryMetricsExporter, ITelemetryTraceExporter
	{
		public void Add(MeterProviderBuilder builder)
		{
			builder.AddConsoleExporter(options => { });
		}

		public void Add(TracerProviderBuilder builder)
		{
			builder.AddConsoleExporter(options => { });
		}
	}

	public class PrometheusExporter : ITelemetryMetricsExporter, IConfigureApp
	{
		public void Add(MeterProviderBuilder builder)
		{
			builder.AddPrometheusExporter();
		}

		public void Configure(IApplicationBuilder app)
		{
			var prometheusEndpoint = "/wherethewhitemetricsat";
			// "https://localhost:7174/metrics" http://localhost:5067/internal/metrics/"; // configuration["Telemetry:Prometheus:ScrapeEndpoint"];
			if (!string.IsNullOrEmpty(prometheusEndpoint))
			{
				var url = new Uri(prometheusEndpoint, UriKind.Relative);
				app.UseOpenTelemetryPrometheusScrapingEndpoint(context =>
				{
					return url.IsAbsoluteUri 
					? context.Request.Path == url.AbsolutePath && context.Connection.LocalPort == url.Port
					: context.Request.Path == url.OriginalString; // Don't expose publicly (configure Urls in appsettings.json)
				});
			}
		}
	}

	public class OtlpExporter : ITelemetryMetricsExporter
	{
		public void Add(MeterProviderBuilder builder)
		{
			builder.AddOtlpExporter(options =>
			 {
				 options.Endpoint = new Uri("http://localhost:4317");
			 });
		}
	}

	public class ApplicationInsightsExporters : ITelemetryMetricsExporter, ITelemetryTraceExporter
	{
		public void Add(MeterProviderBuilder builder)
		{
			//builder.AddApplicationInsightsExporter(options =>
			//{
			//	options.ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
			//});
		}

		public void Add(TracerProviderBuilder builder)
		{
			//builder.AddApplicationInsightsMetricsExporter(options =>
			//{
			//	options.ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
			//});
		}
	}
}