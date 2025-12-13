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

			var db = new MongoClient(runner.ConnectionString).GetDatabase("_mydb");
			var sut = new MongoTrainingRepository(db);
			var training = new Training { Username = "ABV", Created = DateTimeOffset.UtcNow };
			var id = await sut.AddGetId(training);
			var retrieved = await sut.Get(id);

			retrieved?.Username.ShouldBe(training.Username);

			var ugdr = new MongoUserGeneratedDataRepositoryProvider(db, id);

			var phase = new Phase { exercise = "a", phase_type = "TEST", training_day = 1, time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };
			var result = await ugdr.Phases.Upsert([phase]);
			result.Added.Count().ShouldBe(1);
			result.Updated.Count().ShouldBe(0);

			result = await ugdr.Phases.Upsert([phase]);
			result.Added.Count().ShouldBe(0);
			result.Updated.Count().ShouldBe(1);
		}
	}
}
