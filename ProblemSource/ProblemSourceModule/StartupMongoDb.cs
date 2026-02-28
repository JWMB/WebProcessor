using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using ProblemSource.Models.Aggregates;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Models;
using ProblemSourceModule.Models.Aggregates;
using ProblemSourceModule.Services.Storage;
using ProblemSourceModule.Services.Storage.MongoDb;

namespace ProblemSource
{
	public class StartupMongoDb
    {
		public void Configure(IServiceCollection services, IConfiguration config)
		{
			var dbConfig = services.Select(o => o.ImplementationInstance).OfType<MongoTools.MongoConfig>().FirstOrDefault();
			if (dbConfig == null)
			{
				var section = config.GetSection("AppSettings:Storage:MongoDB");
				dbConfig = new MongoTools.MongoConfig(section["ConnectionString"]!, section["Database"]!);
				// "mongodb://localhost:27017/?maxPoolSize=500&waitQueueSize=2500";
			}

			Console.WriteLine($"connectionString={dbConfig.ConnectionString} database={dbConfig.Database}");

			var client = new MongoClient(dbConfig.ConnectionString);
			services.AddSingleton(sp => client.GetDatabase(dbConfig.Database));

			// DocumentBase MongoDocumentWrapper

			//BsonClassMap.RegisterClassMap<SubscriberRequest<MessageType1Request>>(cm => { });
			var wrappedTypes = new[] { typeof(User), typeof(Training), typeof(TrainingSummary) } // hmm, isn't TrainingSummary a MongoTrainingAssociatedDocumentWrapper?
				.Select(o => typeof(MongoDocumentWrapper<>).MakeGenericType(o))
				.Concat(new[] { typeof(TrainingSummary), typeof(TrainingDayAccount), typeof(Phase), typeof(PhaseStatistics) }
				.Select(o => typeof(MongoTrainingAssociatedDocumentWrapper<>).MakeGenericType(o)));
			// +typeof(TDocument)   { Name = "MongoTrainingAssociatedDocumentWrapper`1" FullName = "ProblemSourceModule.Services.Storage.MongoDb.MongoTrainingAssociatedDocumentWrapper`1[[ProblemSourceModule.Models.Aggregates.TrainingSummary, ProblemSourceModule, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]"}

			foreach (var wrappedType in wrappedTypes)
			{
				var cm = new BsonClassMap(wrappedType);
				cm.AutoMap();
				cm.SetIgnoreExtraElements(true);
				BsonClassMap.RegisterClassMap(cm);
			}

			BsonClassMap.RegisterClassMap<object>(cm =>
			{
				cm.SetIgnoreExtraElements(true);
			});

			BsonClassMap.RegisterClassMap<DocumentBase>(cm =>
			{
				cm.AutoMap();
				cm.SetIgnoreExtraElements(true);
				//cm.MapIdProperty(c => c.Id)
				//	.SetIdGenerator(MongoDB.Bson.Serialization.IdGenerators.StringObjectIdGenerator.Instance)
				//	.SetSerializer(new MongoDB.Bson.Serialization.Serializers.StringSerializer(MongoDB.Bson.BsonType.ObjectId));
			});

			BsonSerializer.RegisterSerializer(new XObjectCustomSerializer());

			services.AddSingleton<ITrainingSummaryRepository, MongoTrainingSummaryRepository>();
			services.AddSingleton<IUserRepository, MongoUserRepository>();

			services.AddSingleton<IUserGeneratedDataRepositoryProviderFactory, MongoUserGeneratedDataRepositoryProviderFactory>();

			services.AddSingleton<ITrainingRepository, MongoTrainingRepository>();
		}
	}
}
