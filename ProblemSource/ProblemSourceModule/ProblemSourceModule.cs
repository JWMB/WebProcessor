﻿using Common;
using Common.Web.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PluginModuleBase;
using ProblemSource.Services;
using ProblemSource.Services.Storage;
using ProblemSource.Services.Storage.AzureTables;
using ProblemSourceModule.Services;
using ProblemSourceModule.Services.Storage;
using ProblemSourceModule.Services.Storage.AzureTables;
using ProblemSourceModule.Services.TrainingAnalyzers;
using System.Linq;

namespace ProblemSource
{
    public class ProblemSourceModule : IPluginModule
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ITrainingPlanRepository, EmbeddedTrainingPlanRepository>();
            services.AddSingleton<IAggregationService, AggregationService>();

            services.AddSingleton<ProblemSourceProcessingMiddleware>();
            services.AddHttpClient();

            // training analyzers:
            // TODO: these should be provided by configuration, not hardcoded!
            //var analyzers = new[] { typeof(ExperimentalAnalyzer), typeof(CategorizerDay5_23Q1) };
            var analyzerInterfaceType = typeof(ITrainingAnalyzer);
            var analyzers = analyzerInterfaceType.Assembly.GetTypes()
                .Where(analyzerInterfaceType.IsAssignableFrom)
                .Where(o => !o.IsInterface)
                .ToArray();

            //var pathToMLModel = "Resources/JuliaMLModel_Reg.zip";
            services.AddSingleton<IPredictNumberlineLevelService>(sp =>
                new MLPredictNumberlineLevelService(new RemoteMLPredictor(sp.GetRequiredService<IConfiguration>().GetOrThrow<string>("MLPredictionEndpoint"), sp.GetRequiredService<IHttpClientFactory>()))
                //new MLPredictNumberlineLevelService(new LocalMLPredictor(
                //    sp.GetRequiredService<IWebHostEnvironment>().ContentRootFileProvider.GetFileInfo(pathToMLModel)?.PhysicalPath ?? ""
                //))
                );
            services.AddSingleton<IEnumerable<ITrainingAnalyzer>>(sp => analyzers.Select(o => (ITrainingAnalyzer)sp.GetOrCreateInstance(o)));

            services.AddSingleton<TrainingAnalyzerCollection>();
            //services.AddSingleton<TrainingAnalyzerCollection>(sp => new TrainingAnalyzerCollection(new[] { }, sp.GetRequiredService<>));

            if (services.Any(o => o.ServiceType == typeof(IEventDispatcher)) == false)
                services.AddSingleton<IEventDispatcher, NullEventDispatcher>(); // AzureQueueEventDispatcher>();

            services.AddSingleton<IClientSessionManager, InMemorySessionManager>();
            //            services.AddSingleton<IEventDispatcher>(sp =>
            ////    new QueueEventDispatcher(sp.GetRequiredService<IConfiguration>().GetOrThrow<string>("AppSettings:AzureQueue:ConnectionString"), sp.GetRequiredService<ILogger<QueueEventDispatcher>>()));

            services.AddMemoryCache();

            services.AddSingleton<ITrainingTemplateRepository, StaticTrainingTemplateRepository>();
            ConfigureForAzureTables(services);
            ConfigureUsernameHashing(services);
        }

        public void ConfigureForAzureTables(IServiceCollection services, bool useCaching = true)
        {
            services.AddSingleton<IUserRepository, AzureTableUserRepository>();
            services.AddSingleton<ITypedTableClientFactory, TypedTableClientFactory>();
            services.UpsertSingleton<ITableClientFactory>(sp => sp.GetRequiredService<ITypedTableClientFactory>());

            if (useCaching)
                services.AddSingleton<IUserGeneratedDataRepositoryProviderFactory, CachingAzureTableUserGeneratedDataRepositoriesProviderFactory>();
            else
                services.AddSingleton<IUserGeneratedDataRepositoryProviderFactory, AzureTableUserGeneratedDataRepositoriesProviderFactory>();

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

            var queueEventDispatcher = serviceProvider.GetService<IEventDispatcher>() as AzureQueueEventDispatcher;
            queueEventDispatcher?.Init().Wait();
        }
    }
}
