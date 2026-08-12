using Common.Web.Services;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PluginModuleBase;
//using OpenTelemetry.Logs;
//using OpenTelemetry.Resources;
//using OpenTelemetry.Trace;

namespace Common.Web
{
    public static class ServiceConfiguration
    {
        public static void ConfigureProcessingPipelineServices(IServiceCollection services, IConfiguration config, IEnumerable<IPluginModule> pluginModules)
        {
            services.AddSingleton<ITableClientFactory, TableClientFactory>(); //(sp => new TableClientFactory("vektor")
            services.AddSingleton<IDataSink, AzureTableLogSink>();
            services.AddSingleton<IProcessingMiddlewarePipelineRepository, ProcessingPipelineRepository>();
            services.AddSingleton<SinkProcessingMiddleware>();

            foreach (var plugin in pluginModules)
                plugin.ConfigureServices(services, config);
        }

        public static void ConfigurePlugins(IApplicationBuilder app, IEnumerable<IPluginModule> pluginModules)
        {
            var sp = app.ApplicationServices;
            sp.GetRequiredService<IProcessingMiddlewarePipelineRepository>().Register("default", sp.GetRequiredService<SinkProcessingMiddleware>());

            foreach (var plugin in pluginModules)
                plugin.Configure(app);
        }

        public static void ConfigureApplicationInsights(IApplicationBuilder app, IConfiguration config, bool isDevelopment)
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

        public static void ConfigureOtel(IServiceCollection services, IConfiguration config)
        {
            //config["Otel:"]
			//var endpoint = "http://localhost:3000";

			services.AddOpenTelemetry()
                .ConfigureResource(builder => builder.AddDetector(sp => sp.GetRequiredService<MyResourceDetector>()))
                //.ConfigureResource(builder => builder.AddService(serviceName: "MyService"))
                .WithTracing(builder =>
                    builder.AddAspNetCoreInstrumentation()
                        .AddConsoleExporter()
                        //.AddOtlpExporter(opts => { opts.Endpoint = new Uri(endpoint); });
                ) // 
				.WithMetrics(builder =>
                    builder.AddAspNetCoreInstrumentation()
                        .AddConsoleExporter()
					    //.AddOtlpExporter(opts => { opts.Endpoint = new Uri(endpoint); });
				);
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
