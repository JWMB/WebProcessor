using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using ProblemSource.Models;
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
		public static class MongoRegister
		{
			private static bool inited;


			private static class DocumentTypes
			{
				public static Type[] Basic = [typeof(User), typeof(Training), typeof(TrainingSummary)];
				public static Type[] TrainingAssociated = [typeof(TrainingSummary), typeof(TrainingDayAccount), typeof(Phase), typeof(PhaseStatistics), typeof(UserGeneratedState)];


				public static IEnumerable<Type> WrappedBasic = Basic.Select(o => typeof(MongoDocumentWrapper<>).MakeGenericType(o)).ToList();
				public static IEnumerable<Type> WrappedTrainingAssociated = TrainingAssociated.Select(o => typeof(MongoTrainingAssociatedDocumentWrapper<>).MakeGenericType(o)).ToList();

				public static IEnumerable<Type> AllWrapped = WrappedBasic.Concat(WrappedTrainingAssociated);

				public static async Task CreateIndices(IMongoDatabase db)
				{
					await CreateIndexBasic(db, Builders<MongoDocumentWrapper<User>>.IndexKeys.Descending(o => o.Document.Email));
					await CreateIndexBasic(db, Builders<MongoDocumentWrapper<Training>>.IndexKeys.Descending(o => o.Document.Id));
					await CreateIndexBasic(db, Builders<MongoDocumentWrapper<TrainingSummary>>.IndexKeys.Descending(o => o.Document.Id));

					await CreateIndexTrainingAssociated(db, Builders<MongoTrainingAssociatedDocumentWrapper<TrainingSummary>>.IndexKeys.Descending(o => o.TrainingId));
					await CreateIndexTrainingAssociated(db, Builders<MongoTrainingAssociatedDocumentWrapper<TrainingDayAccount>>.IndexKeys.Descending(o => o.TrainingId));
					await CreateIndexTrainingAssociated(db, Builders<MongoTrainingAssociatedDocumentWrapper<Phase>>.IndexKeys.Descending(o => o.TrainingId));
					await CreateIndexTrainingAssociated(db, Builders<MongoTrainingAssociatedDocumentWrapper<PhaseStatistics>>.IndexKeys.Descending(o => o.TrainingId));
					await CreateIndexTrainingAssociated(db, Builders<MongoTrainingAssociatedDocumentWrapper<UserGeneratedState>>.IndexKeys.Descending(o => o.TrainingId));
				}

				private static readonly string WrappedIdIndexName = "MainIndex";
				private static async Task<bool> HasIndex<T>(IMongoCollection<T> collection, string name)
				{
					var existing = await (await collection.Indexes.ListAsync()).ToListAsync();
					return existing.Any(o => o["name"].AsString == WrappedIdIndexName);
				}

				public static async Task CreateIndexTrainingAssociated<T>(IMongoDatabase db, IndexKeysDefinition<MongoTrainingAssociatedDocumentWrapper<T>> indices)
				{
					var collection = db.GetCollection<MongoTrainingAssociatedDocumentWrapper<T>>(typeof(T).Name);
					if (await HasIndex(collection, WrappedIdIndexName))
						return;
					var indexModel = new CreateIndexModel<MongoTrainingAssociatedDocumentWrapper<T>>(indices, new CreateIndexOptions { Name = WrappedIdIndexName });
					collection.Indexes.CreateOne(indexModel);
				}

				public static async Task CreateIndexBasic<T>(IMongoDatabase db, IndexKeysDefinition<MongoDocumentWrapper<T>> indices)
				{
					var collection = db.GetCollection<MongoDocumentWrapper<T>>(typeof(T).Name);
					if (await HasIndex(collection, WrappedIdIndexName))
						return;
					var indexModel = new CreateIndexModel<MongoDocumentWrapper<T>>(indices, new CreateIndexOptions { Name = WrappedIdIndexName });
					collection.Indexes.CreateOne(indexModel);
				}
			}

			public static void Run()
			{
				if (inited) return;
				inited = true;

				//BsonClassMap.RegisterClassMap<SubscriberRequest<MessageType1Request>>(cm => { });

				foreach (var wrappedType in DocumentTypes.AllWrapped)
				{
					var cm = new BsonClassMap(wrappedType);
					cm.AutoMap();
					cm.SetIgnoreExtraElements(true);
					try
					{
						BsonClassMap.RegisterClassMap(cm);
					}
					catch (ArgumentException ex) when (ex.Message.Contains("An item with the same key"))
					{ }
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
			}

			public static async Task EnsureIndices(IMongoClient client, MongoTools.MongoConfig config)
			{
				var db = client.GetDatabase(config.Database);
				await DocumentTypes.CreateIndices(db);
			}
		}

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
			MongoRegister.Run();
			MongoRegister.EnsureIndices(client, dbConfig).Wait();

			services.AddSingleton<ITrainingSummaryRepository, MongoTrainingSummaryRepository>();
			services.AddSingleton<IUserRepository, MongoUserRepository>();

			services.AddSingleton<IUserGeneratedDataRepositoryProviderFactory, MongoUserGeneratedDataRepositoryProviderFactory>();

			services.AddSingleton<ITrainingRepository, MongoTrainingRepository>();
		}
	}
}
