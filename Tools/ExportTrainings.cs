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
			if (!directory.Exists)
				directory.Create();
			foreach (var id in trainingIds)
				await ExportTrainingStats(id, directory);
		}

		private async Task ExportTrainingStats(int trainingId, DirectoryInfo directory)
		{
			var ugdr = dataRepositoryProviderFactory.Create(trainingId); // (new AzureTableUserGeneratedDataRepositoriesProviderFactory(tableClientFactory)).Create(trainingId);
			//var trainingRepo = new ProblemSourceModule.Services.Storage.AzureTables.AzureTableTrainingRepository(tableClientFactory);

			try
			{
				var exportData = new Export
				{
					Training = await trainingRepository.Get(trainingId),
					TrainingSummary = (await ugdr.TrainingSummaries.GetAll()).SingleOrDefault(),
					TrainingDayAccount = (await ugdr.TrainingDays.GetAll()).ToList(),
					PhaseStatistics = (await ugdr.PhaseStatistics.GetAll()).ToList(),
					//Phases = await ugdr.Phases.GetAll(),
				};
				var json = Newtonsoft.Json.JsonConvert.SerializeObject(exportData, Newtonsoft.Json.Formatting.Indented);

				var filename = $"{trainingId}_{exportData.Training?.Username}.json";
				await File.WriteAllTextAsync(Path.Join(directory.FullName, filename), json);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}

		public class Export
		{
			public Training? Training { get; set; }
			public TrainingSummary? TrainingSummary { get; set; }
			public List<TrainingDayAccount>? TrainingDayAccount { get; set; }
			public List<PhaseStatistics>? PhaseStatistics { get; set; }
			public List<Phase>? Phases { get; set; }
		}

		public async Task Import(string json, int? targetTrainingId = null, bool forceCreateNewTraining = false)
		{
			var export = Newtonsoft.Json.JsonConvert.DeserializeObject<Export>(json);
			if (export == null)
				throw new Exception("Could not parse");

			if (export == null || export.Training == null)
				throw new Exception("Empty export");

			if (targetTrainingId.HasValue)
			{
				throw new NotImplementedException();
			}
			else
			{
				var found = forceCreateNewTraining ? null : await trainingRepository.GetByUsername(export.Training.Username);
				if (found == null)
				{
					await trainingRepository.Add(export.Training);
				}
				else
					export.Training.Id = found.Id;
			}

			if (export.TrainingSummary != null)
				export.TrainingSummary.Id = export.Training.Id;
			foreach (var item in export.TrainingDayAccount ?? [])
			{
				item.AccountId = export.Training.Id;
				item.AccountUuid = export.Training.Username;
			}
			foreach (var item in export.PhaseStatistics ?? [])
				item.account_id = export.Training.Id;
			//foreach (var item in export.Phases ?? [])
			//	item.id = export.Training.Id;

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
