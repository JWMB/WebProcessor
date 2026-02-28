using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using PluginModuleBase;
using ProblemSource.Services;
using ProblemSource.Services.Storage;
using ProblemSource.Services.Storage.AzureTables;
using ProblemSourceModule.Models;
using ProblemSourceModule.Services;
using ProblemSourceModule.Services.Storage;
using ProblemSourceModule.Services.Storage.MongoDb;
using ProblemSourceModule.Services.TrainingAnalyzers;

namespace ProblemSource
{
    public class ProblemSourceModule : IPluginModule
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration config)
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

            var pathToMLModel = @"Resources\JuliaMLModel_Reg.zip"; // TODO: config
            if (pathToMLModel != null)
            {
				var paths = new[] { pathToMLModel }.ToList();
				var di = new FileInfo(GetType().Assembly.Location).Directory;
                if (di != null)
                    paths.Add(Path.Join(di.FullName, pathToMLModel));
                pathToMLModel = paths.FirstOrDefault(o => File.Exists(o));
				if (pathToMLModel != null)
                {
                    services.AddSingleton<IPredictNumberlineLevelService>(sp =>
                        //new MLPredictNumberlineLevelService(new RemoteMLPredictor(sp.GetRequiredService<IConfiguration>().GetOrThrow<string>("MLPredictionEndpoint"), sp.GetRequiredService<IHttpClientFactory>()))
                        new MLPredictNumberlineLevelService(new LocalMLPredictor(
							pathToMLModel
							//sp.GetRequiredService<IWebHostEnvironment>().ContentRootFileProvider.GetFileInfo(pathToMLModel)?.PhysicalPath ?? ""
                        ))
                        );
                }
            }
			services.AddSingleton(sp => analyzers.Select(o => (ITrainingAnalyzer)sp.GetOrCreateInstance(o)));

            services.AddSingleton<TrainingAnalyzerCollection>();
            //services.AddSingleton<TrainingAnalyzerCollection>(sp => new TrainingAnalyzerCollection(new[] { }, sp.GetRequiredService<>));

            services.AddSingleton<IClientSessionManager, InMemorySessionManager>();

			services.AddMemoryCache();

            services.AddSingleton<ITrainingTemplateRepository, StaticTrainingTemplateRepository>();

        	var storageIsMongo = false;
			config ??= services.Select(o => o.ImplementationInstance).OfType<IConfigurationRoot>().Single();
			{
				if (config != null)
					storageIsMongo = config["AppSettings:Storage:Type"] == "MongoDB";
            }

            if (storageIsMongo)
				new StartupMongoDb().Configure(services, config!);
            else
				new StartupAzureStorage().Configure(services);

			if (services.Any(o => o.ServiceType == typeof(IEventDispatcher)) == false)
				services.AddSingleton<IEventDispatcher, NullEventDispatcher>(); // TODO: shouldn't be needed (nullable in ProblemSourceProcessingMiddleware)

			ConfigureUsernameHashing(services);
        }


		public void ConfigureUsernameHashing(IServiceCollection services)
        {
            services.AddSingleton(new MnemoJapanese(2));
            services.AddSingleton(sp => new UsernameHashing(sp.GetRequiredService<MnemoJapanese>(), 2));
            services.AddSingleton<ITrainingUsernameService, MnemoJapaneseTrainingUsernameService>();
        }

        public void Configure(IApplicationBuilder app)
             => Configure(app, true);

		public void Configure(IApplicationBuilder app, bool initAzureStorage)
        {
            var serviceProvider = app.ApplicationServices;
            serviceProvider.GetService<IProcessingMiddlewarePipelineRepository>()?
                .Register("problemsource", serviceProvider.GetRequiredService<ProblemSourceProcessingMiddleware>());

            if (initAzureStorage)
            {
                // Initializing TableClientFactory on startup, in order to get an early error:
                var tableClientFactory = serviceProvider.GetService<ITypedTableClientFactory>() as TypedTableClientFactory;
                tableClientFactory?.Init().Wait();

                var queueEventDispatcher = serviceProvider.GetService<IEventDispatcher>() as AzureQueueEventDispatcher;
                queueEventDispatcher?.Init().Wait();
            }

			var storageConfig = serviceProvider.GetService<StorageConfig>();
            if (storageConfig?.Users?.Any() == true)
            {
				var userRepo = serviceProvider.GetService<IUserRepository>();
                if (userRepo != null)
                {
                    var users = userRepo.GetAll().Result;
                    if (!users.Any())
                    {
                        foreach (var item in storageConfig.Users)
                        {
                            item.PasswordForHashing = item.HashedPassword;
							userRepo.Upsert(item).Wait();
						}
					}
				}
			}
		}
    }

	public class StorageConfig
	{
		public string Type { get; set; } = "";
		public List<User> Users { get; set; } = [];
		public MongoTools.MongoConfig MongoDB { get; set; } = new("", "");
	}
}
