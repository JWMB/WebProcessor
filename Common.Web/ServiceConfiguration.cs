using Common.Web.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PluginModuleBase;

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
	}
}
