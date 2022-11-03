using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PluginModuleBase;
using ProblemSource.Services;
using ProblemSource.Services.Storage;
using ProblemSource.Services.Storage.AzureTables;

namespace ProblemSource
{
    public class ProblemSourceModule : IPluginModule
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IUserStateRepository, AzureTableUserStateRepository>(); //AzureTableUserStateRepository InMemoryUserStateRepository
            //services.AddSingleton<ITrainingPlanRepository>(sp => new TrainingPlanRepository(new DirectoryInfo(sp.GetRequiredService<IHostEnvironment>().ContentRootPath)));
            services.AddSingleton<ITrainingPlanRepository, TrainingPlanRepository>();
            services.AddSingleton<ProblemSourceProcessingPipeline>();
            services.AddSingleton<IEventDispatcher>(sp => 
                new QueueEventDispatcher(sp.GetRequiredService<IConfiguration>()["AppSettings:AzureQueue:ConnectionString"], sp.GetRequiredService<ILogger<QueueEventDispatcher>>())); 
            //QueueEventDispatcher NullEventDispatcher
            services.AddSingleton<IAggregationService, AggregationService>(); // AggregationService NullAggregationService

            services.AddSingleton<ITableClientFactory>(sp => new TableClientFactory(sp.GetRequiredService<IConfiguration>()["AppSettings:AzureTable:ConnectionString"]));
            //var tableFactory = TableClientFactory.Create("").Result;
            //services.AddSingleton<ITableClientFactory>(sp => tableFactory);
            services.AddSingleton<IUserGeneratedDataRepositoryProviderFactory, AzureTableUserGeneratedDataRepositoriesProviderFactory>();
        }

        public void Configure(IServiceProvider serviceProvider)
        {
            serviceProvider.GetRequiredService<IProcessingPipelineRepository>().Register("problemsource", serviceProvider.GetRequiredService<ProblemSourceProcessingPipeline>());

            // Initializing TableClientFactory on startup, in order to get an early error:
            var tableClientFactory = serviceProvider.GetService<ITableClientFactory>() as TableClientFactory;
            tableClientFactory?.Init().Wait();

            var queueEventDispatcher = serviceProvider.GetService<IEventDispatcher>() as QueueEventDispatcher;
            queueEventDispatcher?.Init().Wait();
        }
    }
}
