using Microsoft.Extensions.DependencyInjection;
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
            services.AddSingleton<IUserStateRepository, InMemoryUserStateRepository>(); //AzureTableUserStateRepository
            //services.AddSingleton<ITrainingPlanRepository>(sp => new TrainingPlanRepository(new DirectoryInfo(sp.GetRequiredService<IHostEnvironment>().ContentRootPath)));
            services.AddSingleton<ITrainingPlanRepository, TrainingPlanRepository>();
            services.AddSingleton<ProblemSourceProcessingPipeline>();
            services.AddSingleton<IEventDispatcher, QueueEventDispatcher>(); //QueueEventDispatcher NullEventDispatcher
            services.AddSingleton<IAggregationService, AggregationService>(); // NullAggregationService AggregationService

            var tableFactory = new TableClientFactory();
            try
            {
                tableFactory.Init().Wait();
            }
            catch (Exception ex)
            {
                if (ex.ToString().Contains("127.0.0.1:"))
                    throw new Exception("Could not connect to Storage Emulator - have you started it? See Azurite, https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio");
                throw;
            }
            services.AddSingleton<ITableClientFactory>(sp => tableFactory);
            services.AddSingleton<AzureTableUserGeneratedDataRepositoriesProviderFactory>();
        }

        public void Configure(IServiceProvider serviceProvider)
        {
            serviceProvider.GetRequiredService<IProcessingPipelineRepository>().Register("problemsource", serviceProvider.GetRequiredService<ProblemSourceProcessingPipeline>());
        }
    }
}
