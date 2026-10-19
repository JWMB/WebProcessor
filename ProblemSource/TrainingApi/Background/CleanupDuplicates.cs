using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using ProblemSource.Models.Aggregates;
using ProblemSourceModule.Models;
using ProblemSourceModule.Models.Aggregates;
using ProblemSourceModule.Services.Storage.MongoDb;

namespace TrainingApi.Background
{
	public class CleanupDuplicates : BackgroundProcessorBase
	{
		private readonly IMongoDatabase database;
		private readonly ILogger<CleanupDuplicates> log;

		public class Config : ProcessorConfigBase
		{ }
		public CleanupDuplicates(Config config, IMongoDatabase database, ILogger<CleanupDuplicates> log) : base(config)
		{
			this.database = database;
			this.log = log;
		}

		protected override async Task<BackgroundProcessorResult> ProcessInner()
		{
			var collection = database.GetCollection<MongoDocumentWrapper<Training>>("Training");
			//var trainings = await (await collection.FindAsync(o => true)).ToListAsync();
			var trainings = await collection.AsQueryable().Select(o => new { o.Document.Id, o.Document.Username }).ToListAsync();

			foreach (var training in trainings)
			{
				var duplicates = await FindDuplicateTrainingData(training.Id, database);
				if (duplicates.IsEmpty == false)
				{
					log.LogInformation($"Training {training.Id}: delete {duplicates.ToString()}");
				}
			}

			return new BackgroundProcessorResult(true);
		}

		public class DuplicateSets
		{
			public List<DupInfo> PhaseStatistics { get; set; } = new();
			public List<DupInfo> TrainingSummary { get; set; } = new();
			public List<DupInfo> TrainingDayAccount { get; set; } = new();

			public bool IsEmpty => PhaseStatistics.Any() == false && TrainingSummary.Any() == false && TrainingDayAccount.Any() == false;
			public override string ToString()
			{
				return string.Join("\n", new[] { PhaseStatistics, TrainingSummary, TrainingDayAccount }.Select(DupInfo.ToString).Where(o => o.Any()));
			}
		}

		public record DupInfo(List<ObjectId> Ids, Type Type, string Key)
		{
			public override string ToString() => Ids.Any() ? $"'{Key}'/{Ids.Count}" : "";
			public static string ToString(IEnumerable<DupInfo> items) => items.Any() ? $"{items.First().Type.Name}: {string.Join(", ", items.Select(o => o.ToString()))}" : "";
		}
		public static async Task<DuplicateSets> FindDuplicateTrainingData(int trainingId, IMongoDatabase db)
		{
			var result = new DuplicateSets();
			var collPs = db.GetCollection<MongoTrainingAssociatedDocumentWrapper<PhaseStatistics>>("PhaseStatistics");
			var ps = await (await collPs.FindAsync(o => o.TrainingId == trainingId)).ToListAsync();

			var uniquePs = ps.GroupBy(o => $"{o.Document.training_day} {o.Document.exercise} {o.Document.timestamp}").ToDictionary(o => o.Key,
				td => td.OrderByDescending(o => o.Document.num_questions).ToList());

			var toDeletePs = uniquePs.Where(o => o.Value.Count > 1).Select(o => new DupInfo(o.Value.Skip(1).Select(p => p.Id).ToList(), typeof(PhaseStatistics), o.Key)).ToList();
			if (toDeletePs.Any())
				result.PhaseStatistics = toDeletePs;

			var collTs = db.GetCollection<MongoTrainingAssociatedDocumentWrapper<TrainingSummary>>("TrainingSummary");
			var ts = await (await collTs.FindAsync(o => o.TrainingId == trainingId)).ToListAsync();
			var aa = ts.OrderByDescending(o => o.Document.TrainedDays).ThenBy(o => o.Document.LastLogin).ToList();
			var toDeleteTs = aa.Skip(1).Select(o => o.Id).ToList();
			if (toDeleteTs.Any())
				result.TrainingSummary = [new DupInfo(toDeleteTs, typeof(TrainingSummary), "")];

			var collTda = db.GetCollection<MongoTrainingAssociatedDocumentWrapper<TrainingDayAccount>>("TrainingDayAccount");
			var tda = await (await collTda.FindAsync(o => o.TrainingId == trainingId)).ToListAsync();
			var uniqueTda = tda.GroupBy(o => $"{o.Document.TrainingDay} {o.Document.StartTime}").ToDictionary(o => o.Key,
				td => td.OrderByDescending(o => o.Document.NumQuestions).ToList());
			var toDeleteTda = uniqueTda.Where(o => o.Value.Count > 1).Select(o => new DupInfo(o.Value.Skip(1).Select(p => p.Id).ToList(), typeof(TrainingDayAccount), o.Key )).ToList();
			if (toDeleteTda.Any())
				result.TrainingDayAccount = toDeleteTda;

			return result;
		}
	}
}
