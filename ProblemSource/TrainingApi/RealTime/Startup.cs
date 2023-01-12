using Microsoft.AspNetCore.Builder;

namespace TrainingApi.RealTime
{
    public class Startup // TODO: can we somehow utilize IStartupFilter?
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IConnectionsRepository, ConnectionsRepository>();
            services.AddSingleton<QueueListener>();
            services.AddSingleton<CommHubWrapper>();
            services.AddHostedService(sp => new TimedHostedService(sp.GetRequiredService<QueueListener>().Receive, sp.GetRequiredService<ILogger<TimedHostedService>>()));
        }

        public void Configure(WebApplication app, string path)
        {   
            app.MapHub<CommHub>(path);
        }

        //public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        //{
        //    return builder =>
        //    {
        //        if (builder is WebApplication webApplication)
        //            Configure(webApplication);
        //        next(builder);
        //    };
        //}
    }
}