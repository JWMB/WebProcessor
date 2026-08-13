using ProblemSource.Models;
using ProblemSource.Models.Aggregates;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Models;
using ProblemSourceModule.Models.Aggregates;
using ProblemSourceModule.Services.Storage;
using System.Text;

namespace ProblemSource.Services
{
	public interface ITrainingImporter
	{
		Task Import(TrainingExport export, int? targetTrainingId = null, bool forceCreateNewTraining = false);
	}

	public class TrainingExport
	{
		public Training? Training { get; set; }
		public TrainingSummary? TrainingSummary { get; set; }
		public List<TrainingDayAccount>? TrainingDayAccount { get; set; }
		public List<PhaseStatistics>? PhaseStatistics { get; set; }
		public List<Phase>? Phases { get; set; }
		public UserGeneratedState? UserState { get; set; }
	}

	public class ImportExportTrainings : ITrainingImporter
	{
		private readonly IUserGeneratedDataRepositoryProviderFactory dataRepositoryProviderFactory;
		private readonly ITrainingRepository trainingRepository;

		public ImportExportTrainings(IUserGeneratedDataRepositoryProviderFactory dataRepositoryProviderFactory, ITrainingRepository trainingRepository)
		{
			this.dataRepositoryProviderFactory = dataRepositoryProviderFactory;
			this.trainingRepository = trainingRepository;
		}

		public async Task ExportStats(IEnumerable<int> trainingIds, DirectoryInfo directory)
		{
			if (!directory.Exists)
				directory.Create();
			foreach (var id in trainingIds)
				await ExportStats(id, directory, false);
		}

		public async Task ExportStats(int trainingId, DirectoryInfo directory, bool includeDetails)
		{
			var ugdr = dataRepositoryProviderFactory.Create(trainingId); // (new AzureTableUserGeneratedDataRepositoriesProviderFactory(tableClientFactory)).Create(trainingId);
																		 //var trainingRepo = new ProblemSourceModule.Services.Storage.AzureTables.AzureTableTrainingRepository(tableClientFactory);

			try
			{
				var exportData = new TrainingExport
				{
					Training = await trainingRepository.Get(trainingId),
					TrainingSummary = (await ugdr.TrainingSummaries.GetAll()).SingleOrDefault(),
					TrainingDayAccount = (await ugdr.TrainingDays.GetAll()).ToList(),
					PhaseStatistics = (await ugdr.PhaseStatistics.GetAll()).ToList(),
					Phases = includeDetails ? (await ugdr.Phases.GetAll()).ToList() : null,
					UserState = includeDetails ? (await ugdr.UserStates.GetAll()).SingleOrDefault() : null,
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

		public async Task Import(string json, int? targetTrainingId = null, bool forceCreateNewTraining = false)
		{
			var export = Newtonsoft.Json.JsonConvert.DeserializeObject<TrainingExport>(json);
			if (export == null)
				throw new Exception("Could not parse");
			await Import(export, targetTrainingId, forceCreateNewTraining);
		}

		public async Task Import(TrainingExport export, int? targetTrainingId = null, bool forceCreateNewTraining = false)
		{
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
			//if (export.UserState != null)
			//	export.UserState.

			var ugdr = dataRepositoryProviderFactory.Create(export.Training.Id);

			await ugdr.RemoveAll();

			if (export.TrainingSummary != null)
				await ugdr.TrainingSummaries.Upsert([export.TrainingSummary]);

			if (export.TrainingDayAccount?.Any() == true)
				await ugdr.TrainingDays.Upsert(export.TrainingDayAccount);

			if (export.PhaseStatistics?.Any() == true)
				await ugdr.PhaseStatistics.Upsert(export.PhaseStatistics);

			if (export.Phases?.Any() == true)
				await ugdr.Phases.Upsert(export.Phases);

			if (export.UserState != null)
				await ugdr.UserStates.Upsert([export.UserState]);
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

		public async Task MergeInFolder(DirectoryInfo directory)
		{
			var mergedFileName = "merged.json";
			var files = directory.GetFiles("*.json");
			var merged = new StringBuilder();
			merged.Append("[\n");
			foreach (var file in files.Where(o => o.Name != mergedFileName))
			{
				var json = await File.ReadAllTextAsync(file.FullName);
				merged.Append(json);
				merged.Append("\n,\n");
			}
			merged.Append("\n]");
			var filename = Path.Join(directory.FullName, mergedFileName);
			await File.WriteAllTextAsync(filename, merged.ToString());
		}
	}
}
