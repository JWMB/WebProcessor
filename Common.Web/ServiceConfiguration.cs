using Common.Web.Services;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PluginModuleBase;

namespace Common.Web
{
    public static class ServiceConfiguration
    {
        public static void ConfigureProcessingPipelineServices(IServiceCollection services, IEnumerable<IPluginModule> pluginModules)
        {
            services.AddSingleton<ITableClientFactory, TableClientFactory>(); //(sp => new TableClientFactory("vektor")
            services.AddSingleton<IDataSink, AzureTableLogSink>();
            services.AddSingleton<IProcessingMiddlewarePipelineRepository, ProcessingPipelineRepository>();
            services.AddSingleton<SinkProcessingMiddleware>();

            foreach (var plugin in pluginModules)
                plugin.ConfigureServices(services);
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
                    throw new ArgumentException("InstrumentationKey not set");
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
