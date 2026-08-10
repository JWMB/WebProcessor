using ProblemSource.Services.Storage.AzureTables;

namespace Tools
{
	internal class ExportTrainings
	{
		private readonly ITypedTableClientFactory tableClientFactory;

		public ExportTrainings(ITypedTableClientFactory tableClientFactory)
		{
			this.tableClientFactory = tableClientFactory;
		}

		public async Task ExportStats(IEnumerable<int> trainingIds, DirectoryInfo directory)
		{
			foreach (var id in trainingIds)
				await ExportTrainingStats(id, directory);
		}
		public async Task ExportStats(IEnumerable<string> trainingUuids, DirectoryInfo directory)
		{
			var ids = new List<int>();
			foreach (var uuid in trainingUuids)
			{
				var id = await TrainingNormCreator.GetTrainingId(tableClientFactory, uuid);
				if (id == null)
					throw new Exception($"Not found: {uuid}");
				ids.Add(id.Value);
			}
			await ExportStats(ids, directory);
		}

		private async Task ExportTrainingStats(int trainingId, DirectoryInfo directory)
		{
			var ugdr = (new AzureTableUserGeneratedDataRepositoriesProviderFactory(tableClientFactory)).Create(trainingId);

			var exportData = new
			{
				//Phases = await ugdr.Phases.GetAll(),
				PhaseStatistics = await ugdr.PhaseStatistics.GetAll(),
				TrainingDayAccount = await ugdr.TrainingDays.GetAll(),
			};
			var json = Newtonsoft.Json.JsonConvert.SerializeObject(exportData);

			var filename = $"{trainingId}.json";
			await File.WriteAllTextAsync(Path.Join(directory.FullName, filename), json);
		}
	}
}
