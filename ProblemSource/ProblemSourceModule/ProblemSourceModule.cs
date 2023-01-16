using Common;
using Common.Web.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PluginModuleBase;
using ProblemSource.Services;
using ProblemSource.Services.Storage;
using ProblemSource.Services.Storage.AzureTables;
using ProblemSourceModule.Services;
using ProblemSourceModule.Services.Storage;
using ProblemSourceModule.Services.Storage.AzureTables;

namespace ProblemSource
{
    public class ProblemSourceModule : IPluginModule
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ITrainingPlanRepository, EmbeddedTrainingPlanRepository>();
            services.AddSingleton<IAggregationService, AggregationService>();

            services.AddSingleton<ProblemSourceProcessingMiddleware>();
            services.AddSingleton<IEventDispatcher, NullEventDispatcher>();

            services.AddSingleton<IClientSessionManager, InMemorySessionManager>();
            //            services.AddSingleton<IEventDispatcher>(sp =>
            ////    new QueueEventDispatcher(sp.GetRequiredService<IConfiguration>().GetOrThrow<string>("AppSettings:AzureQueue:ConnectionString"), sp.GetRequiredService<ILogger<QueueEventDispatcher>>()));

            services.AddMemoryCache();

            ConfigureForAzureTables(services);
            ConfigureUsernameHashing(services);
        }

        public void ConfigureForAzureTables(IServiceCollection services)
        {
            services.AddSingleton<IUserRepository, AzureTableUserRepository>();
            services.AddSingleton<ITypedTableClientFactory, TypedTableClientFactory>();
            services.UpsertSingleton<ITableClientFactory>(sp => sp.GetRequiredService<ITypedTableClientFactory>());

            services.AddSingleton<IUserGeneratedDataRepositoryProviderFactory, CachingAzureTableUserGeneratedDataRepositoriesProviderFactory>(); //AzureTableUserGeneratedDataRepositoriesProviderFactory

            services.AddSingleton<ITrainingRepository, AzureTableTrainingRepository>();
        }

        public void ConfigureUsernameHashing(IServiceCollection services)
        {
            services.AddSingleton(new MnemoJapanese(2));
            services.AddSingleton(sp => new UsernameHashing(sp.GetRequiredService<MnemoJapanese>(), 2));
            services.AddSingleton<ITrainingUsernameService, MnemoJapaneseTrainingUsernameService>();
        }

        public void Configure(IApplicationBuilder app)
        {
            var serviceProvider = app.ApplicationServices;
            serviceProvider.GetService<IProcessingMiddlewarePipelineRepository>()?
                .Register("problemsource", serviceProvider.GetRequiredService<ProblemSourceProcessingMiddleware>());

            // Initializing TableClientFactory on startup, in order to get an early error:
            var tableClientFactory = serviceProvider.GetService<ITypedTableClientFactory>() as TypedTableClientFactory;
            tableClientFactory?.Init().Wait();

            var queueEventDispatcher = serviceProvider.GetService<IEventDispatcher>() as QueueEventDispatcher;
            queueEventDispatcher?.Init().Wait();
        }
    }

    public static class IServiceCollectionExtensions
    {
        public static void UpsertSingleton<TService>(this IServiceCollection services)
            where TService : class
            => services.Upsert<TService>(() => services.AddSingleton<TService>());

        public static void UpsertSingleton<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory)
            where TService : class
            =>
            services.Upsert<TService>(() => services.AddSingleton(sp => implementationFactory(sp)));

        public static void UpsertSingleton<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
            =>
            services.Upsert<TService, TImplementation>(() => services.AddSingleton<TService, TImplementation>());


        public static void Upsert<TService, TImplementation>(this IServiceCollection services, Action add)
            where TService : class
            where TImplementation : class, TService
        {
            services.RemoveService<TService>();
            services.RemoveService<TImplementation>();
            add();
        }
        public static void Upsert<TService>(this IServiceCollection services, Action add)
    where TService : class
        {
            services.RemoveService<TService>();
            add();
        }


        public static bool RemoveService<T>(this IServiceCollection services)
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
