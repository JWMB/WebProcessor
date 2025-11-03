using AngleSharp.Common;
using Azure.Data.Tables;
using Common;
using Force.DeepCloner;
using ProblemSource.Models.Aggregates;
using ProblemSource.Services;
using ProblemSource.Services.Storage.AzureTables;
using ProblemSourceModule.Models;
using ProblemSourceModule.Models.Aggregates;
using ProblemSourceModule.Services.Storage.AzureTables;

namespace Tools
{
	public class TrainingNormCreator
	{
		private readonly IStatisticsProvider statisticsProvider;
		private readonly ITypedTableClientFactory tableClientFactory;

		public TrainingNormCreator(IStatisticsProvider statisticsProvider, ITypedTableClientFactory tableClientFactory)
		{
			this.statisticsProvider = statisticsProvider;
			this.tableClientFactory = tableClientFactory;
		}

		public static async Task<int?> GetTrainingId(ITypedTableClientFactory tableClientFactory, string username)
		{
			var q = tableClientFactory.Trainings.QueryAsync<TableEntity>($"Username eq '{username}'", 1);
			await foreach (var t in q.AsPages())
			{
				if (t.Values.Any())
					return int.Parse(t.Values[0]["Id"]!.ToString()!);
				break;
			}
			return null;
		}

		private async Task RecreateTraining(Training training)
		{
			var targetNormTrainingId = await GetTrainingId(tableClientFactory, training.Username);
			if (targetNormTrainingId.HasValue)
			{
				training.Id = targetNormTrainingId.Value;
				var ugdr = (new AzureTableUserGeneratedDataRepositoriesProviderFactory(tableClientFactory)).Create(training.Id);
				await ugdr.RemoveAll();
			}
			else
			{
				var trainingRepo = new AzureTableTrainingRepository(tableClientFactory);
				targetNormTrainingId = await trainingRepo.Add(training);
				training.Id = targetNormTrainingId.Value;
			}
		}

		public async Task Create(IEnumerable<Training> trainings, Training targetTraining)
		{
			Console.WriteLine($"Creating norm training for {targetTraining.Username}...");
			Console.WriteLine("Getting summaries...");
			var summaries = (await statisticsProvider.GetTrainingSummaries(trainings.Select(o => o.Id))).OfType<TrainingSummary>();

			var allStats = new Dictionary<Training, List<PhaseStatistics>>();

			var allPhaseStats = new List<PhaseStatistics>();
			var allTrainingDays = new List<TrainingDayAccount>();
			summaries = summaries.Where(o => o.TrainedDays > 7).ToList();
			Console.WriteLine($"Found {summaries.Count()} summaries...");
			foreach (var (index, summary) in summaries.Index())
			{
				if (index % 50 == 0)
					Console.WriteLine($"{index + 1}/{summaries.Count()}: {summary.Id} (days={summary.TrainedDays})");
				var phaseStatistics = (await statisticsProvider.GetPhaseStatistics(summary.Id)).ToList();

				var toMaxDay = phaseStatistics.Where(o => o.training_day <= MaxDay).ToList();
				allTrainingDays.AddRange(TrainingDayAccount.Create(summary.Id, toMaxDay));

				allPhaseStats.AddRange(toMaxDay);
			}

			await RecreateTraining(targetTraining);

			var targetTrainingStdDev = targetTraining.DeepClone();
			targetTrainingStdDev.Username += "_stddev";
			await RecreateTraining(targetTrainingStdDev);

			foreach (var item in new[] { 
				(Training: targetTraining, Func: (Func<IEnumerable<decimal>, decimal>)(lst => lst.Average())),
				(Training: targetTrainingStdDev, Func: lst => lst.StdDev()),
			})
			{
				Console.WriteLine($"{item.Training.Username}={item.Training.Id}");
				var ugdr = (new AzureTableUserGeneratedDataRepositoriesProviderFactory(tableClientFactory)).Create(item.Training.Id);
				await ugdr.PhaseStatistics.Upsert(CreateAggregatePhaseStatistics(allPhaseStats, item.Training, item.Func));
				await ugdr.TrainingDays.Upsert(CreateAggregateTrainingDays(allTrainingDays, trainings, item.Training, item.Func));

				//var trainingSummary = TrainingSummary.Create(targetNormTrainingId.Value, trainingDays);
				//await repos.TrainingSummaries.Upsert(new[] { trainingSummary });
			}
		}

		private int MaxDay = 40;
		private DateTime GetDateTimeForTrainingDay(int trainingDay)
		{
			return DateTime.Today.AddDays(-MaxDay + trainingDay);
		}

		private IEnumerable<TrainingDayAccount> CreateAggregateTrainingDays(IEnumerable<TrainingDayAccount> allTrainingDays, IEnumerable<Training> trainings, Training targetTraining, Func<IEnumerable<decimal>, decimal> statFunc) //phaseStatistics
		{
			var trainingsById = trainings.ToDictionary(o => o.Id);
			return allTrainingDays.GroupBy(o => o.TrainingDay).Select(byDay =>
			{
				var lst = byDay.ToList();
				var respMins = byDay.Select(o => 1M * o.ResponseMinutes).ToList();
				var avg = respMins.Average();
				var sd = respMins.StdDev();

				var tmp = lst.Where(o => Math.Abs(o.ResponseMinutes - avg) < sd * 2).ToList();
				if (tmp.Any())
					lst = tmp;
				var remainingMinutesExceptSlackers = lst
					.Select(o => new { Total = o.RemainingMinutes + o.ResponseMinutes, o.RemainingMinutes, Target = trainingsById.GetValueOrDefault(o.AccountId)?.Settings?.timeLimits?[0] ?? 33M })
					.Where(o => o.Target > 0 && (o.Total < (1.5M * o.Target)))
					.Select(o => 1M * o.RemainingMinutes).ToList();

				return new TrainingDayAccount
				{
					AccountId = targetTraining.Id,
					TrainingDay = byDay.Key,
					AccountUuid = targetTraining.Username,
					StartTime = GetDateTimeForTrainingDay(byDay.Key),
					EndTimeStamp = GetDateTimeForTrainingDay(byDay.Key), // TODO:

					ResponseMinutes = (int)statFunc(lst.Select(o => (decimal)o.ResponseMinutes)),
					RemainingMinutes = remainingMinutesExceptSlackers.Any() ? (int)statFunc(remainingMinutesExceptSlackers) : (int)statFunc(lst.Select(o => (decimal)o.RemainingMinutes)),
					NumCorrectAnswers = (int)statFunc(lst.Select(o => (decimal)o.NumCorrectAnswers)),
					NumQuestions = (int)statFunc(lst.Select(o => (decimal)o.NumQuestions)),
				};
			});
		}

		private IEnumerable<PhaseStatistics> CreateAggregatePhaseStatistics(IEnumerable<PhaseStatistics> allPhaseStats, Training targetTraining, Func<IEnumerable<decimal>, decimal> statFunc) //phaseStatistics
		{
			return allPhaseStats.GroupBy(o => o.training_day).SelectMany(
				byDay => byDay.GroupBy(o => o.exercise).Select(byEx => 
				{
					var lst = byEx.ToList();
					if (lst.Count >= 10)
					{
						var maxLevels = lst.Select(q => q.level_max).ToList();
						var avg = maxLevels.Average();
						var sd = maxLevels.StdDev();

						var tmp = lst.Where(q => Math.Abs(q.level_max - avg) < sd * 2).ToList();
						if (tmp.Any())
							lst = tmp;
					}

					var result = new PhaseStatistics
					{
						account_id = targetTraining.Id,
						training_day = byDay.Key,
						exercise = byEx.Key,
						timestamp = GetDateTimeForTrainingDay(byDay.Key),
						end_timestamp = GetDateTimeForTrainingDay(byDay.Key),
					};
					if (lst.Any())
					{
						result.level_max = Math.Round(statFunc(lst.Select(o => o.level_max)), 2);
						result.level_min = Math.Round(statFunc(lst.Select(o => o.level_min)), 2);
						result.end_timestamp += TimeSpan.FromMinutes((double)statFunc(lst.Select(q => (decimal)(q.end_timestamp - q.timestamp).TotalMinutes)));
						result.response_time_total = (int)statFunc(lst.Select(q => (decimal)q.response_time_total));
						result.num_correct_answers = (int)statFunc(lst.Select(q => (decimal)q.num_correct_answers));
						result.num_incorrect_answers = (int)statFunc(lst.Select(q => (decimal)q.num_incorrect_answers));
						result.num_questions = (int)statFunc(lst.Select(q => (decimal)q.num_questions));
					}
					return result;
				})
				).ToList();
		}
	}
}
