using ProblemSource.Models.Aggregates;
using ProblemSource.Models;
using ProblemSource.Services.Storage;
using Microsoft.Extensions.Logging;
using ProblemSourceModule.Models.Aggregates;

namespace ProblemSource.Services
{
    public interface IAggregationService
    {
        Task UpdateAggregates(IUserGeneratedDataRepositoryProvider repos, List<LogItem> logItems, int userId);
    }

    public class AggregationService : IAggregationService
    {
        private readonly ILogger<AggregationService> log;

        public AggregationService(ILogger<AggregationService> log)
        {
            this.log = log;
        }

        public async Task UpdateAggregates(IUserGeneratedDataRepositoryProvider repos, List<LogItem> logItems, int userId)
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

                // later we could look into optimizing, (as in don't re-run aggregation from scratch each time)
                var phaseStats = PhaseStatistics.Create(0, await phaseRepo.GetAll());
                await repos.PhaseStatistics.Upsert(phaseStats);

                var trainingDays = TrainingDayAccount.Create(userId, await repos.PhaseStatistics.GetAll());
                await repos.TrainingDays.Upsert(trainingDays);

                var trainingSummary = TrainingSummary.Create(userId, await repos.TrainingDays.GetAll());
                await repos.TrainingSummaries.Upsert(new[] { trainingSummary });
            }
            catch (Azure.Data.Tables.TableTransactionFailedException ex)
            {
                log.LogError($"UpdateAggregates", ex);
            }
        }
    }
}
