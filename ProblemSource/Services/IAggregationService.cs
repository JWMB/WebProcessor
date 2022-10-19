using ProblemSource.Models.Aggregates;
using ProblemSource.Models;
using ProblemSource.Services.Storage;

namespace ProblemSource.Services
{
    public interface IAggregationService
    {
        Task UpdateAggregates(IUserGeneratedDataRepositoryProvider repos, List<LogItem> logItems, string userId);
    }

    public class NullAggregationService : IAggregationService
    {
        public Task UpdateAggregates(IUserGeneratedDataRepositoryProvider repos, List<LogItem> logItems, string userId) => Task.CompletedTask;
    }

    public class AggregationService : IAggregationService
    {
        public async Task UpdateAggregates(IUserGeneratedDataRepositoryProvider repos, List<LogItem> logItems, string userId)
        {
            // TODO: Move to async service (Azure (Durable) Functions maybe)?
            // But if we want to adapt response depending on some analysis... Then it's easier to handle it right here (instead of waiting for separate service)

            var phaseRepo = repos.Phases;
            var phasesResult = LogEventsToPhases.Create(logItems, await phaseRepo.GetAll());
            if (phasesResult.Errors?.Any() == true)
            {
            }
            await phaseRepo.AddOrUpdate(phasesResult.AllPhases);

            // later we could look into optimizing, (as in don't re-run aggregation from scratch each time)
            var phaseStats = PhaseStatistics.Create(0, await phaseRepo.GetAll());
            await repos.PhaseStatistics.AddOrUpdate(phaseStats);

            var trainingDays = TrainingDayAccount.Create(userId, 0, await repos.PhaseStatistics.GetAll()); // await phaseRepo.GetAll());
            await repos.TrainingDays.AddOrUpdate(trainingDays);
        }
    }
}
