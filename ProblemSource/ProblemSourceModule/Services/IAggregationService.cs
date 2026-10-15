using ProblemSource.Models.Aggregates;
using ProblemSource.Models;
using ProblemSource.Services.Storage;
using Microsoft.Extensions.Logging;
using ProblemSourceModule.Models.Aggregates;
using ProblemSourceModule.Services;

namespace ProblemSource.Services
{
    public interface IAggregationService
    {
        Task UpdateAggregates(IUserGeneratedDataRepositoryProvider repos, List<LogItem> logItems, int userId);
        Task UpdateAggregatesFromPhases(IUserGeneratedDataRepositoryProvider repos, IEnumerable<Phase> phases, int trainingId);
    }

    public class AggregationService : IAggregationService
    {
        private readonly ILogger<AggregationService> log;
		private readonly IStreamingStatistics? streamingStatistics;

		public AggregationService(ILogger<AggregationService> log, IStreamingStatistics? streamingStatistics = null)
        {
            this.log = log;
			this.streamingStatistics = streamingStatistics;
		}

        public async Task UpdateAggregates(IUserGeneratedDataRepositoryProvider repos, List<LogItem> logItems, int trainingId)
        {
            // TODO: Move to async service (Azure (Durable) Functions maybe)?
            // But if we want to adapt response depending on some analysis... Then it's easier to handle it right here (instead of waiting for separate service)

            var phaseRepo = repos.Phases;
            var phasesResult = LogEventsToPhases.Create(logItems, await phaseRepo.GetAll());
            if (phasesResult.Errors?.Any() == true)
            {
                log.LogError($"{string.Join("\n", phasesResult.Errors)}");
            }

            try
            {
                await phaseRepo.Upsert(phasesResult.PhasesUpdated.Concat(phasesResult.PhasesCreated));
                await UpdateAggregatesFromPhases(repos, await phaseRepo.GetAll(), trainingId);

				if (streamingStatistics != null)
				{
					try
					{
						await streamingStatistics.Add(trainingId, phasesResult.PhasesCreated);
					}
					catch (Exception ex)
					{
						log.LogWarning($"streamingStatistics error, trainingId:{trainingId}", ex);
					}
				}
			}
            catch (Azure.Data.Tables.TableTransactionFailedException ex)
            {
                log.LogError($"UpdateAggregates error, trainingId:{trainingId}", ex);
            }
        }

        public async Task<IEnumerable<TrainingDayAccount>> TempRecalcTrainingDayAccount(IUserGeneratedDataRepositoryProvider repos, IStatisticsProvider statisticsProvider, int trainingId)
        {
            var phases = await repos.Phases.GetAll();
            var grouped = phases.GroupBy(o => o.training_day).ToDictionary(o => o.Key,
                    td => td.GroupBy(o => o.exercise).ToDictionary(o => o.Key,
                        x => x.GroupBy(o => o.time).ToDictionary(o => o.Key,
                        t => t.OrderByDescending(o => o.problems.Count).ThenBy(o => o.problems.LastOrDefault()?.answers.Count ?? 0).First()
                        )
                    )
                );
            var simple = grouped.Values.SelectMany(td => td.Values.SelectMany(x => x.Select(t => t.Value))).OrderBy(o => o.time).ToList();
            var phaseStats = PhaseStatistics.Create(0, simple);
			//var ps = await statisticsProvider.GetPhaseStatistics(trainingId);
			return TrainingDayAccount.Create(trainingId, phaseStats);
		}

        public async Task UpdateAggregatesFromPhases(IUserGeneratedDataRepositoryProvider repos, IEnumerable<Phase> phases, int trainingId)
        {
			// later we could look into optimizing, (as in don't re-run aggregation from scratch each time)
			var phaseStats = PhaseStatistics.Create(0, phases);
            await repos.PhaseStatistics.Upsert(phaseStats);

			var trainingDays = TrainingDayAccount.Create(trainingId, await repos.PhaseStatistics.GetAll());
            await repos.TrainingDays.Upsert(trainingDays);

            var trainingSummary = TrainingSummary.Create(trainingId, await repos.TrainingDays.GetAll());
            await repos.TrainingSummaries.Upsert(new[] { trainingSummary });
        }
    }
}
