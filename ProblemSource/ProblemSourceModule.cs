using Microsoft.Extensions.DependencyInjection;
using PluginModuleBase;
using ProblemSource.Services;
using ProblemSource.Services.Storage;

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
                    throw new Exception("Could not connect to Azure Storage Emulator - have you started it?");
                throw;
            }
            services.AddSingleton<ITableClientFactory>(sp => tableFactory);
            services.AddSingleton<UserGeneratedDataRepositoriesProviderFactory>();
        }

        public void Configure(IServiceProvider serviceProvider)
        {
            serviceProvider.GetRequiredService<IProcessingPipelineRepository>().Register("problemsource", serviceProvider.GetRequiredService<ProblemSourceProcessingPipeline>());
        }
    }
}
