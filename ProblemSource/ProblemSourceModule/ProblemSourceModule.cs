using Common.Web.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PluginModuleBase;
using ProblemSource.Models.LogItems;
using ProblemSource.Services;
using ProblemSource.Services.Storage;
using ProblemSource.Services.Storage.AzureTables;
using ProblemSourceModule.Services.Storage;
using ProblemSourceModule.Services.Storage.AzureTables;

namespace ProblemSource
{
    public class ProblemSourceModule : IPluginModule
    {
        public void ConfigureServices(IServiceCollection services)
        {
            ConfigureServices(services, true);
        }

        public void ConfigureServices(IServiceCollection services, bool configureForPipeline)
        {
            if (configureForPipeline)
                ConfigurePipeline(services);

            //services.AddSingleton<ITrainingPlanRepository>(sp => new TrainingPlanRepository(new DirectoryInfo(sp.GetRequiredService<IHostEnvironment>().ContentRootPath)));
            services.AddSingleton<ITrainingPlanRepository, TrainingPlanRepository>();

            services.AddSingleton<IAggregationService, AggregationService>(); // AggregationService NullAggregationService

            ConfigureForAzureTables(services);
            ConfigureUsernameHashing(services);
        }

        public void ConfigurePipeline(IServiceCollection services)
        {
            services.AddSingleton<IClientSessionManager, InMemorySessionManager>();
            services.AddSingleton<ProblemSourceProcessingMiddleware>();
            services.AddSingleton<IEventDispatcher>(sp =>
                new QueueEventDispatcher(sp.GetRequiredService<IConfiguration>()["AppSettings:AzureQueue:ConnectionString"], sp.GetRequiredService<ILogger<QueueEventDispatcher>>()));
            //QueueEventDispatcher NullEventDispatcher
        }

        public void ConfigureForAzureTables(IServiceCollection services)
        {
            services.AddSingleton<IUserStateRepository, AzureTableUserStateRepository>(); //AzureTableUserStateRepository InMemoryUserStateRepository

            services.AddSingleton<ITypedTableClientFactory, TypedTableClientFactory>();
            RemoveService<ITableClientFactory>(services);
            services.AddSingleton<ITableClientFactory>(sp => sp.GetRequiredService<ITypedTableClientFactory>());

            services.AddSingleton<IUserGeneratedDataRepositoryProviderFactory, AzureTableUserGeneratedDataRepositoriesProviderFactory>();

            services.AddSingleton<ITrainingRepository, AzureTableTrainingRepository>();
        }


        public void ConfigureUsernameHashing(IServiceCollection services)
        {
            services.AddSingleton(new MnemoJapanese(2));
            services.AddSingleton(sp => new UsernameHashing(sp.GetRequiredService<MnemoJapanese>(), 2));
        }

        public void Configure(IServiceProvider serviceProvider)
        {
            serviceProvider.GetService<IProcessingMiddlewarePipelineRepository>()?
                .Register("problemsource", serviceProvider.GetRequiredService<ProblemSourceProcessingMiddleware>());

            // Initializing TableClientFactory on startup, in order to get an early error:
            var tableClientFactory = serviceProvider.GetService<ITypedTableClientFactory>() as TypedTableClientFactory;
            tableClientFactory?.Init().Wait();

            var queueEventDispatcher = serviceProvider.GetService<IEventDispatcher>() as QueueEventDispatcher;
            queueEventDispatcher?.Init().Wait();
        }

        private static bool RemoveService<T>(IServiceCollection services)
        {
            var serviceDescriptor = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(T));
            if (serviceDescriptor != null)
            {
                services.Remove(serviceDescriptor);
                return true;
            }
            return false;
        }
    }
}
