using Common.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using ProblemSource.Services.Storage;
using ProblemSource.Services.Storage.AzureTables;
using ProblemSourceModule.Services.Storage;
using ProblemSourceModule.Services.Storage.AzureTables;
using ProblemSourceModule.Services.Storage.MongoDb;

namespace ProblemSource
{
	public class StartupAzureStorage
	{
		public void Configure(IServiceCollection services, bool useCaching = true)
		{
			services.AddSingleton<IUserRepository, AzureTableUserRepository>();
			services.AddSingleton<ITypedTableClientFactory, TypedTableClientFactory>();
			services.UpsertSingleton<ITableClientFactory>(sp => sp.GetRequiredService<ITypedTableClientFactory>());

			services.AddSingleton<ITrainingSummaryRepository, MongoTrainingSummaryRepository>();


			if (useCaching)
				services.AddSingleton<IUserGeneratedDataRepositoryProviderFactory, CachingAzureTableUserGeneratedDataRepositoriesProviderFactory>();
			else
				services.AddSingleton<IUserGeneratedDataRepositoryProviderFactory, AzureTableUserGeneratedDataRepositoriesProviderFactory>();

			services.AddSingleton<ITrainingRepository, AzureTableTrainingRepository>();

			//services.AddSingleton<IEventDispatcher>(sp =>
			//	new QueueEventDispatcher(sp.GetRequiredService<IConfiguration>().GetOrThrow<string>("AppSettings:AzureQueue:ConnectionString"), sp.GetRequiredService<ILogger<QueueEventDispatcher>>()));
		}
	}
}
