using ProblemSource.Services;

namespace TrainingApi.RealTime
{
    public class Startup // TODO: can we somehow utilize IStartupFilter?
    {
        public void ConfigureServices(IServiceCollection services)
        {
            throw new NotImplementedException("No non-Azure services implemented");
            services.AddSingleton<IQueueClient, AzureQueueClient>();

            services.AddSignalR();
            services.AddSingleton<IConnectionsRepository, ConnectionsRepository>();
            services.AddSingleton<QueueListener>();
            services.AddSingleton<CommHubWrapper>();
            services.AddHostedService(sp => new TimedHostedService(sp.GetRequiredService<QueueListener>().Receive, sp.GetRequiredService<ILogger<TimedHostedService>>()));

            //var registered = services.FirstOrDefault(o => o.ServiceType == typeof(IEventDispatcher));
            //if (registered != null)
            //    services.Remove(registered);
            services.AddSingleton<IEventDispatcher, AzureQueueEventDispatcher>();
		}

		public void Configure(WebApplication app, string pathPattern)
        {
            // TODO: read up on https://learn.microsoft.com/en-us/aspnet/core/signalr/hubs?view=aspnetcore-7.0
            app.MapHub<CommHub>(pathPattern, options =>
            {
                options.MinimumProtocolVersion = 0;
            });
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