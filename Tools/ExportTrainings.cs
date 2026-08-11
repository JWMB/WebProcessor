using ProblemSource.Models.Aggregates;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Models;
using ProblemSourceModule.Models.Aggregates;
using ProblemSourceModule.Services.Storage;

namespace Tools
{
	internal class ExportTrainings
	{
		//private readonly ITypedTableClientFactory tableClientFactory;
		private readonly IUserGeneratedDataRepositoryProviderFactory dataRepositoryProviderFactory;
		private readonly ITrainingRepository trainingRepository;

		public ExportTrainings(IUserGeneratedDataRepositoryProviderFactory dataRepositoryProviderFactory, ITrainingRepository trainingRepository)
		{
			//this.tableClientFactory = tableClientFactory;
			this.dataRepositoryProviderFactory = dataRepositoryProviderFactory;
			this.trainingRepository = trainingRepository;
		}

		public async Task ExportStats(IEnumerable<int> trainingIds, DirectoryInfo directory)
		{
			foreach (var id in trainingIds)
				await ExportTrainingStats(id, directory);
		}
		//public async Task ExportStats(IEnumerable<string> trainingUuids, DirectoryInfo directory)
		//{
		//	var ids = new List<int>();
		//	foreach (var uuid in trainingUuids)
		//	{
		//		var id = await TrainingNormCreator.GetTrainingId(tableClientFactory, uuid);
		//		if (id == null)
		//			throw new Exception($"Not found: {uuid}");
		//		ids.Add(id.Value);
		//	}
		//	await ExportStats(ids, directory);
		//}

		private async Task ExportTrainingStats(int trainingId, DirectoryInfo directory)
		{
			var ugdr = dataRepositoryProviderFactory.Create(trainingId); // (new AzureTableUserGeneratedDataRepositoriesProviderFactory(tableClientFactory)).Create(trainingId);
			//var trainingRepo = new ProblemSourceModule.Services.Storage.AzureTables.AzureTableTrainingRepository(tableClientFactory);
			var exportData = new Export
			{
				Training = await trainingRepository.Get(trainingId),
				TrainingSummary = (await ugdr.TrainingSummaries.GetAll()).Single(),
				TrainingDayAccount = (await ugdr.TrainingDays.GetAll()).ToList(),
				PhaseStatistics = (await ugdr.PhaseStatistics.GetAll()).ToList(),
				//Phases = await ugdr.Phases.GetAll(),
			};
			var json = Newtonsoft.Json.JsonConvert.SerializeObject(exportData);

			var filename = $"{trainingId}.json";
			await File.WriteAllTextAsync(Path.Join(directory.FullName, filename), json);
		}

		public class Export
		{
			public Training? Training { get; set; }
			public TrainingSummary? TrainingSummary { get; set; }
			public List<TrainingDayAccount>? TrainingDayAccount { get; set; }
			public List<PhaseStatistics>? PhaseStatistics { get; set; }
			public List<Phase>? Phases { get; set; }
		}

		public async Task Import(string json)
		{
			var export = Newtonsoft.Json.JsonConvert.DeserializeObject<Export>(json);
			if (export == null)
				throw new Exception("Could not parse");

			if (export == null || export.Training == null)
				throw new Exception("Empty export");

			var found = await trainingRepository.GetByUsername(export.Training.Username);
			if (found == null)
			{
				await trainingRepository.Add(export.Training);
			}

			var ugdr = dataRepositoryProviderFactory.Create(export.Training.Id);

			await ugdr.RemoveAll();

			if (export.TrainingSummary != null)
			{
				await ugdr.TrainingSummaries.Upsert([export.TrainingSummary]);
			}
			if (export.TrainingDayAccount?.Any() == true)
			{
				await ugdr.TrainingDays.Upsert(export.TrainingDayAccount);
			}
			if (export.PhaseStatistics?.Any() == true)
			{
				await ugdr.PhaseStatistics.Upsert(export.PhaseStatistics);
			}
			if (export.Phases?.Any() == true)
			{
				await ugdr.Phases.Upsert(export.Phases);
			}
		}

		public async Task ImportFromFolder(DirectoryInfo directory)
		{
			var files = directory.GetFiles("*.json");
			foreach (var file in files)
			{
				try
				{
					var json = await File.ReadAllTextAsync(file.FullName);
					await Import(json);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error: {ex.Message}");
					continue;
				}
			}
		}
	}
}
