//using EphemeralMongo;
using Mongo2Go;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using ProblemSource.Models.Aggregates;
using ProblemSourceModule.Models;
using ProblemSourceModule.Services.Storage.MongoDb;
using Shouldly;

namespace ProblemSourceModule.Tests
{
    public class MongoDbTests
    {
        [Fact]
        public async Task X()
        {
			using var runner = MongoDbRunner.StartForDebugging(additionalMongodArguments: "--quiet --logpath /dev/null");

			//var options = new MongoRunnerOptions
			//{
			//	UseSingleNodeReplicaSet = true,
			//	//StandardOuputLogger = Console.WriteLine,
			//	StandardOutputLogger = Console.WriteLine,
			//	StandardErrorLogger = Console.WriteLine,
			//};

			//BsonClassMap.RegisterClassMap<MongoDocumentWrapper<Training>>(x =>
			//{
			//	x.AutoMap();
			//	x.GetMemberMap(m => m.Id).SetIgnoreIfDefault(true);
			//});

			var client = new MongoClient(runner.ConnectionString);
			var dbName = "_mydb";
			await client.DropDatabaseAsync(dbName);
			var db = client.GetDatabase(dbName);
			var sut = new MongoTrainingRepository(db);
			var training = new Training { Username = "ABV", Created = DateTimeOffset.UtcNow };
			var id = await sut.AddGetId(training);
			var retrieved = await sut.Get(id);

			retrieved?.Username.ShouldBe(training.Username);

			var collections = await (await db.ListCollectionsAsync()).ToListAsync();

			var phaseA = new Phase { exercise = "a", phase_type = "TEST", training_day = 1, time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };

			var batchRepo = new MongoTrainingBatchRepository<Phase, string>(db, Phase.UniqueIdWithinUser, id);
			var collectionName = batchRepo.GetCollection().CollectionNamespace.CollectionName;
			await db.DropCollectionAsync(collectionName);
			//collections = await (await db.ListCollectionsAsync()).ToListAsync();

			var result = await batchRepo.Upsert([phaseA]);
			result.Added.Count().ShouldBe(1);
			result.Updated.Count().ShouldBe(0);

			var underlyingCollection = batchRepo.GetCollection();
			var count = await underlyingCollection.CountDocumentsAsync(o => true);
			count.ShouldBe(1);

			var phaseB = new Phase { exercise = "a", phase_type = "TEST", training_day = 2, time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };

			result = await batchRepo.Upsert([phaseA, phaseB]);
			result.Added.Count().ShouldBe(1);
			result.Updated.Count().ShouldBe(1);
			count = await underlyingCollection.CountDocumentsAsync(o => true);
			count.ShouldBe(2);


			//await MongoTools.DropCollection<MongoDocumentWrapper<Phase>>(db);
			//var phaseColl = MongoTools.GetCollection<MongoDocumentWrapper<Phase>>(db);

			//var allPhases = await (await phaseColl.FindAsync(o => true)).ToListAsync();

			//var ugdr = new MongoUserGeneratedDataRepositoryProvider(db, id);

			//var result = await ugdr.Phases.Upsert([phase]);
			//result.Added.Count().ShouldBe(1);
			//result.Updated.Count().ShouldBe(0);

			//result = await ugdr.Phases.Upsert([phase]);
			//result.Added.Count().ShouldBe(0);
			//result.Updated.Count().ShouldBe(1);
		}


	}
}
