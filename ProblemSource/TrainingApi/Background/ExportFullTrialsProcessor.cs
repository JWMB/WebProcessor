using Microsoft.Extensions.FileProviders;
using MongoDB.Driver;
using ProblemSource.Models.Aggregates;
using ProblemSourceModule.Models;
using ProblemSourceModule.Services.Storage;
using System.IO.Compression;
using System.Text.RegularExpressions;
using TrainingApi.Services;

namespace TrainingApi.Background
{
	public class ExportFullTrialsProcessor : BackgroundProcessorBase
	{
		public class Config : ProcessorConfigBase
		{
			public required string ExportFilePath { get; set; }
			public List<int>? TrainingIdFilter { get; set; }
		}

		private readonly Config config;
		private readonly IMongoDatabase database;
		private readonly IFileProvider fileProvider;

		public ExportFullTrialsProcessor(Config config, IMongoDatabase database, IFileProvider fileProvider)
			: base(config)
		{
			this.config = config;
			this.database = database;
			this.fileProvider = fileProvider;
		}

		protected override async Task<BackgroundProcessorResult> ProcessInner()
		{
			var file = fileProvider.GetFileInfo(config.ExportFilePath);
			if (file.PhysicalPath == null)
				return new BackgroundProcessorResult(false, Error: $"No physical path available for '{config.ExportFilePath}'");

			var rows = await GetExportRows(new User { Email = "system", Role = Roles.SuperAdmin, Trainings = new() }, config.TrainingIdFilter, database);
			if (!rows.Any())
				return new BackgroundProcessorResult(false, Error: $"No data to export {(config.TrainingIdFilter != null ? string.Join(",", config.TrainingIdFilter) : "")}");

			if (file.Name.ToLower().EndsWith(".zip"))
				await WriteZip(rows, file.PhysicalPath);
			else
				await WriteTsv(rows, file.PhysicalPath);

			return new BackgroundProcessorResult(true, $"{rows.Count()} rows exported to {file.PhysicalPath}");
		}

		public static async Task<User?> FindOwningUser(IEnumerable<int> trainingIds, IUserRepository userRepository)
		{
			// Find first teacher who has any of the ids and use their email as filename
			var allUsers = await userRepository.GetAll();
			return allUsers.FirstOrDefault(o => o.Trainings.GetAllIds().Intersect(trainingIds).Any());
		}

		public static async Task<IEnumerable<TrialDataExportRow>> GetExportRows(User user, IEnumerable<int>? trainingIds, IMongoDatabase database, long? maxRows = null)
		{
			var userForFilename = user;

			var globalPermission = user.Role == Roles.Admin || user.Role == Roles.SuperAdmin;
			if (trainingIds?.Any() == true)
			{
				if (!globalPermission)
				{
					var intersect = user.Trainings.GetAllIds().Intersect(trainingIds);
					if (intersect.Count() < trainingIds.Count())
						throw new ArgumentException("Trainings not owned!");
					trainingIds = intersect.ToList();
				}
			}
			else if (!globalPermission)
				trainingIds = user.Trainings.GetAllIds().ToList();

			var collection = database.GetCollection<ProblemSourceModule.Services.Storage.MongoDb.MongoTrainingAssociatedDocumentWrapper<Phase>>("Phase");

			var all = trainingIds == null ? await collection.FindAsync(o => true) : await collection.FindAsync(o => trainingIds.Contains(o.TrainingId));

			var cnt = 0L;
			var byTrainingDayAndPhase = new Dictionary<int, Dictionary<int, Dictionary<string, Phase>>>();
			while (await all.MoveNextAsync())
			{
				if (maxRows.HasValue && cnt > maxRows.Value)
					break;

				var rows = all.Current.ToList();
				foreach (var row in rows)
				{
					if (!byTrainingDayAndPhase.TryGetValue(row.TrainingId, out var byDay))
					{
						byDay = new();
						byTrainingDayAndPhase[row.TrainingId] = byDay;
					}
					if (!byDay.TryGetValue(row.Document.training_day, out var byPhase))
					{
						byPhase = new();
						byDay[row.Document.training_day] = byPhase;
					}
					var phaseId = Phase.UniqueIdWithinUser(row.Document);

					if (maxRows.HasValue && cnt++ > maxRows.Value)
						break;

					if (!byPhase.TryGetValue(phaseId, out var existing))
						byPhase[phaseId] = row.Document;
					else if (row.Document.problems.Count >= existing.problems.Count && row.Document.problems.LastOrDefault()?.answers.Count > existing.problems.LastOrDefault()?.answers.Count)
						byPhase[phaseId] = row.Document;
					else
						Console.WriteLine($"lesser {row.TrainingId} {phaseId}");
				}
			}

			var rxOnlyLetters = new Regex(@"^[a-zA-Z.+]+$");
			var rxWWM = new Regex(@"WordWithMedia \{ ([^}]+)\}");
			var exportRows = byTrainingDayAndPhase.SelectMany(training =>
				training.Value.SelectMany(day =>
					day.Value.SelectMany(phase =>
						phase.Value.problems.Select(pb =>
							new TrialDataExportRow(training.Key, day.Key, phase.Value.exercise, pb.answers.LastOrDefault()?.correct == true,
							phase.Value.time, rxOnlyLetters.IsMatch(pb.problem_string) ? "" : rxWWM.Replace(pb.problem_string, m => $"{m.Groups[1]}"), Math.Round(pb.level, 3), "tp", 0L, pb.answers.LastOrDefault()?.response_time ?? 0, pb.answers.Count)))))
				.ToList();

			return exportRows;
		}

		public static async Task WriteTsv(IEnumerable<TrialDataExportRow> rows, string filename)
		{
			TablularDataHelpers.WriteFile(rows, filename);
		}
		public static async Task WriteZip(IEnumerable<TrialDataExportRow> rows, string filename)
		{
			//var str = TablularDataHelpers.WriteToString(exportRows);
			//return File(System.Text.Encoding.UTF8.GetBytes(str), "text/csv", filename);

			using var memoryStream = new MemoryStream();
			using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true);

			var demoFile = archive.CreateEntry("file.tsv", CompressionLevel.Optimal);
			using var entryStream = demoFile.Open();
			
			{
				using var innerStream = new MemoryStream();
				using var writer = new StreamWriter(innerStream);
				using var csvWriter = new CsvHelper.CsvWriter(writer, TablularDataHelpers.DefaultConfig);
				TablularDataHelpers.Write(rows, csvWriter);
				await writer.FlushAsync();
				await innerStream.FlushAsync();
				innerStream.Seek(0, SeekOrigin.Begin);
				innerStream.CopyTo(entryStream);
			}

			//using var streamWriter = new StreamWriter(entryStream);
			//TablularDataHelpers.Write(rows, entryStream);
			//contentStream.CopyTo(entryStream);
			//streamWriter.Write("Bar!");
			//await entryStream.FlushAsync();
			//await entryStream.DisposeAsync();

			using var fileStream = new FileStream(filename, FileMode.Create);
			memoryStream.Seek(0, SeekOrigin.Begin);
			memoryStream.CopyTo(fileStream);

			//memoryStream.Seek(0, SeekOrigin.Begin);
			//return File(memoryStream.ToArray(), "application/zip", filename);
		}

		public record TrialDataExportRow(
	int account_id,
	int training_day,
	string exercise,
	bool correct,
	long problem_time,
	string problem_string,
	decimal level,
	string training_plan_name,
	long targetTime,
	int response_time,
	int tries
	// bool is_intro
	// numberline_mod
	// weight_mod
	// age_lower
	// predicted_level
	// predicted_tier
	);
	}


	//public class TimedHostedServiceSync(ILogger<TimedHostedServiceSync> logger) : IHostedService, IDisposable
	//{
	//	private int executionCount = 0;
	//	private Timer? _timer = null;

	//	public Task StartAsync(CancellationToken stoppingToken)
	//	{
	//		logger.LogInformation("Timed Hosted Service running.");

	//		_timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

	//		return Task.CompletedTask;
	//	}

	//	private void DoWork(object? state)
	//	{
	//		var count = Interlocked.Increment(ref executionCount);

	//		logger.LogInformation("Timed Hosted Service is working. Count: {Count}", count);
	//	}

	//	public Task StopAsync(CancellationToken stoppingToken)
	//	{
	//		_timer?.Change(Timeout.Infinite, 0);
	//		return Task.CompletedTask;
	//	}

	//	public void Dispose()
	//	{
	//		_timer?.Dispose();
	//	}
	//}

	//internal interface IScopedProcessingService
	//{
	//	Task DoWork(CancellationToken stoppingToken);
	//}

	//internal class ScopedProcessingService(ILogger<ScopedProcessingService> logger) : IScopedProcessingService
	//{
	//	private int executionCount = 0;

	//	public async Task DoWork(CancellationToken stoppingToken)
	//	{
	//		while (!stoppingToken.IsCancellationRequested)
	//		{
	//			executionCount++;
	//			logger.LogInformation("Scoped Processing Service is working. Count: {Count}", executionCount);
	//			await Task.Delay(10000, stoppingToken);
	//		}
	//	}
	//}

	//public class ConsumeScopedServiceHostedService(IServiceProvider services, ILogger<ConsumeScopedServiceHostedService> logger) : BackgroundService
	//{
	//	public IServiceProvider Services { get; } = services;

	//	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	//	{
	//		logger.LogInformation("Consume Scoped Service Hosted Service running.");
	//		await DoWork(stoppingToken);
	//	}

	//	private async Task DoWork(CancellationToken stoppingToken)
	//	{
	//		logger.LogInformation("Consume Scoped Service Hosted Service is working.");
	//		using (var scope = Services.CreateScope())
	//		{
	//			var scopedProcessingService = scope.ServiceProvider.GetRequiredService<IScopedProcessingService>();
	//			await scopedProcessingService.DoWork(stoppingToken);
	//		}
	//	}

	//	public override async Task StopAsync(CancellationToken stoppingToken)
	//	{
	//		logger.LogInformation("Consume Scoped Service Hosted Service is stopping.");
	//		await base.StopAsync(stoppingToken);
	//	}
	//}
}