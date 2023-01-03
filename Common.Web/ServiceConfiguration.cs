using Common.Web.Services;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
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

        public static void ConfigurePlugins(IServiceProvider serviceProvider, IEnumerable<IPluginModule> pluginModules)
        {
            serviceProvider.GetRequiredService<IProcessingMiddlewarePipelineRepository>().Register("default", serviceProvider.GetRequiredService<SinkProcessingMiddleware>());

            foreach (var plugin in pluginModules)
                plugin.Configure(serviceProvider);
        }

        public static void ConfigureApplicationInsights(IServiceProvider serviceProvider, IConfiguration config, bool isDevelopment)
        {
            var aiConn = config.GetValue("ApplicationInsights:ConnectionString", "");
            if (aiConn == "SECRET" || aiConn == string.Empty)
            {
                if (isDevelopment == false)
                    throw new ArgumentException("InstrumentationKey not set");
            }
            else
            {
                var telemetryConfig = serviceProvider.GetService<TelemetryConfiguration>();
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
