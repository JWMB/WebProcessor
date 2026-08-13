using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Common.Web
{
	public class SetupOpenTelemetry
	{
		public static void Add(IServiceCollection services, IConfiguration config)
		{
			//config["Otel:"]
			var endpointLoki = "http://localhost:4318";
			var endpointPrometheus = "/metrics";

			services.AddOpenTelemetry()
				//.ConfigureResource(builder => builder.AddDetector(sp => sp.GetRequiredService<MyResourceDetector>()))
				//.ConfigureResource(builder => builder.AddService(serviceName: "MyService"))
				.WithTracing(builder =>
				{
					builder.AddAspNetCoreInstrumentation()
						.AddConsoleExporter();
					if (endpointLoki?.Any() == true)
						builder.AddOtlpExporter(opts => { opts.Endpoint = new Uri(endpointLoki); });
				})
				.WithMetrics(builder =>
				{
					builder.AddAspNetCoreInstrumentation()
						.AddRuntimeInstrumentation()
						.AddHttpClientInstrumentation()
						.AddConsoleExporter()
						.AddPrometheusExporter(c => c.ScrapeEndpointPath = endpointPrometheus);
					//.AddOtlpExporter(opts => { opts.Endpoint = new Uri(endpoint); });
				});
		}

		public static void Configure(IApplicationBuilder app)
		{
			app.UseOpenTelemetryPrometheusScrapingEndpoint();
		}

		public class MyResourceDetector : IResourceDetector
		{
			private readonly IWebHostEnvironment webHostEnvironment;

			public MyResourceDetector(IWebHostEnvironment webHostEnvironment)
			{
				this.webHostEnvironment = webHostEnvironment;
			}

			public Resource Detect()
			{
				return ResourceBuilder.CreateEmpty()
					.AddService(serviceName: webHostEnvironment.ApplicationName)
					.AddAttributes(new Dictionary<string, object> { ["host.environment"] = webHostEnvironment.EnvironmentName })
					.Build();
			}
		}
	}
}
