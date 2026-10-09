using MongoDB.Driver;
using ProblemSource.Models;
using ProblemSource.Models.Aggregates;
using ProblemSource.Services.Storage;
using ProblemSource.Services.Storage.AzureTables;
using ProblemSourceModule.Models;
using ProblemSourceModule.Services.Storage;
using ProblemSourceModule.Services.Storage.AzureTables;
using ProblemSourceModule.Services.Storage.MongoDb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tools
{
    internal class MigrateToMongoDb
    {
        private readonly ITypedTableClientFactory tableClientFactory;
        private readonly ITrainingRepository trainingRepository;
        private readonly IUserRepository userRepository;
        private readonly IMongoDatabase db;

        //private readonly IUserGeneratedDataRepositoryProvider userGeneratedDataRepositoryProvider;

        // , IUserGeneratedDataRepositoryProvider userGeneratedDataRepositoryProvider
        public MigrateToMongoDb(ITypedTableClientFactory tableClientFactory, ITrainingRepository trainingRepository, IUserRepository userRepository, IMongoDatabase db)
		{
            this.tableClientFactory = tableClientFactory;
            this.trainingRepository = trainingRepository;
            this.userRepository = userRepository;
            this.db = db;
			//this.userGeneratedDataRepositoryProvider = userGeneratedDataRepositoryProvider;
		}

		private async Task Test()
		{
			var mdbRepoFactory = new MongoUserGeneratedDataRepositoryProviderFactory(db);

			var mdbRepos = mdbRepoFactory.Create(1);
			var jobj = new Newtonsoft.Json.Linq.JObject();
			var state = new UserGeneratedState
			{
				exercise_stats = new ExerciseStats
				{
					trainingPlanSettings = new TrainingPlanSettings
					{
						changes = [new TrainingPlanChange { change = new Newtonsoft.Json.Linq.JObject(), timestamp = 1, type = "t" }]
					},
					gameCustomData = new Dictionary<string, object>(),
					gameRuns = [],
					planetInfos = null,
					//metaphorData = new Newtonsoft.Json.Linq.JObject(),
				},
				//syncInfo = new Newtonsoft.Json.Linq.JObject(),
				user_data = null //new Newtonsoft.Json.Linq.JObject() //jobj
			};
			await mdbRepos.UserStates.Upsert([state]);

			var tmp = await mdbRepos.UserStates.GetAll();
		}

		public async Task MigrateTest()
		{
			var training = new Training { Id = 7, Username = "A", TrainingPlanName = "aa", Settings = new TrainingSettings() };
			//var trainings = new[] { };
			var mdbRepoFactory = new MongoUserGeneratedDataRepositoryProviderFactory(db);
			var mdbTrainings = new MongoTrainingRepository(db);

			await mdbTrainings.Upsert(training); //Add
			var mdbRepos = mdbRepoFactory.Create(training.Id);

			var summary = (await mdbRepos.TrainingSummaries.GetAll()).SingleOrDefault();
		}

		public async Task Migrate()
		{
            var mdbRepoFactory = new MongoUserGeneratedDataRepositoryProviderFactory(db);

			Console.WriteLine("Reading users...");
			var users = await userRepository.GetAll();
			var mdbUsers = new MongoUserRepository(db);
			await mdbUsers.Upsert(users);

			//var mdbTrainings = new DbWrappedCollection<Training, int>(db, d => d.Id, d => new MongoDocumentWrapper<Training>(d));
			var mdbTrainings = new MongoTrainingRepository(db);
			Console.WriteLine("Reading trainings...");
			var allMongoTrainings = await mdbTrainings.GetCollection().Find(o => true).Project(o => o.Document.Id).ToListAsync();

			var trainings = (await trainingRepository.GetAll())
				.Where(o => o.Id > 32431)
				.ToList();
			//var trainings = await trainingRepository.GetByIds([3, 7]);
			Console.WriteLine($"{trainings.Count()} trainings found, {allMongoTrainings.Count} in MongoDB");

			//var aaq = new MongoTrainingBatchRepository<Training, int>(db, d => d.Id, trainingId);
			//collection = new DbWrappedCollection<TDocument, TId>(db, getId, item => new MongoTrainingAssociatedDocumentWrapper<TDocument>(item, trainingId, o => getId(o)?.ToString() ?? ""));

			foreach (var (index, training) in trainings.Index())
            {
				var repos = new AzureTableUserGeneratedDataRepositoryProvider(tableClientFactory, training.Id);

				var trainingSummaries = await repos.TrainingSummaries.GetAll();
				var trainedDays = trainingSummaries.SingleOrDefault()?.TrainedDays;
				var skip = trainedDays == null || trainedDays <= 2;
				Console.WriteLine($"{index}/{trainings.Count()} Id={training.Id} TrainedDays={trainingSummaries.SingleOrDefault()?.TrainedDays} {(skip ? "SKIP!" : "")}");
				if (skip)
					continue;
				if (allMongoTrainings.Contains(training.Id))
				{
					Console.WriteLine("Training already in mongo - skip");
					continue;
				}

				await mdbTrainings.Upsert(training); //Add
				var mdbRepos = mdbRepoFactory.Create(training.Id);

				var summary = (await mdbRepos.TrainingSummaries.GetAll()).SingleOrDefault();
				if (summary != null)
				{
					Console.WriteLine("Summary already in mongo - skip");
					continue;
				}

				await mdbRepos.UserStates.Upsert(await repos.UserStates.GetAll());
				await mdbRepos.TrainingSummaries.Upsert(trainingSummaries);
				await mdbRepos.TrainingDays.Upsert(await repos.TrainingDays.GetAll());
				await mdbRepos.PhaseStatistics.Upsert(await repos.PhaseStatistics.GetAll());
				await mdbRepos.Phases.Upsert(await repos.Phases.GetAll());
			}
		}

  //      private async Task X<T>(IBatchRepository<T> repo, IMongoDatabase db)
  //      {
		//	var rows = await repo.GetAll();
  //          var docs = rows.Select(o => new MongoDocumentWrapper<T>(o)).ToList();
		//	new DbWrappedCollection<T, int> collection
		//	//new DbCollection(db, "", () => "asd");

		//	var collection = MongoTools.GetCollection<T>(db);
  //          await collection.InsertManyAsync(docs);
		//}
	}
}
