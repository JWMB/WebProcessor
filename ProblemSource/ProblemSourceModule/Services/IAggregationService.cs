using ProblemSource.Models.Aggregates;
using ProblemSource.Models;
using ProblemSource.Services.Storage;
using Microsoft.Extensions.Logging;

namespace ProblemSource.Services
{
    public interface IAggregationService
    {
        Task UpdateAggregates(IUserGeneratedDataRepositoryProvider repos, List<LogItem> logItems, int userId);
    }

    public class NullAggregationService : IAggregationService
    {
        public Task UpdateAggregates(IUserGeneratedDataRepositoryProvider repos, List<LogItem> logItems, int userId) => Task.CompletedTask;
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
                await phaseRepo.AddOrUpdate(phasesResult.AllPhases);

                // later we could look into optimizing, (as in don't re-run aggregation from scratch each time)
                var phaseStats = PhaseStatistics.Create(0, await phaseRepo.GetAll());
                await repos.PhaseStatistics.AddOrUpdate(phaseStats);

                var trainingDays = TrainingDayAccount.Create(userId, await repos.PhaseStatistics.GetAll()); // await phaseRepo.GetAll());
                await repos.TrainingDays.AddOrUpdate(trainingDays);
            }
            catch (Azure.Data.Tables.TableTransactionFailedException ex) // FullName = "Azure.Data.Tables.TableTransactionFailedException"}
            {
                log.LogError($"UpdateAggregates", ex);
            }
        }
    }
}
