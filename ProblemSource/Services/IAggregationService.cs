using ProblemSource.Models.Aggregates;
using ProblemSource.Models;
using ProblemSource.Services.Storage;

namespace ProblemSource.Services
{
    public interface IAggregationService
    {
        Task UpdateAggregates(IUserGeneratedRepositories repos, List<LogItem> logItems, string userId);
    }

    public class NullAggregationService : IAggregationService
    {
        public Task UpdateAggregates(IUserGeneratedRepositories repos, List<LogItem> logItems, string userId) => Task.CompletedTask;
        //public Task UpdateAggregates(Session session, List<LogItem> logItems, string userId) => Task.CompletedTask;
    }

    public class AggregationService : IAggregationService
    {
        public async Task UpdateAggregates(IUserGeneratedRepositories repos, List<LogItem> logItems, string userId)
        {
            // TODO: Move to async service (Azure (Durable) Functions maybe)?
            // But if we want to adapt response depending on some analysis... Then it's easier to handle it right here (instead of waiting for separate service)

            var phaseRepo = repos.Phases;
            var phasesResult = LogEventsToPhases.Create(logItems, await phaseRepo.GetAll());
            if (phasesResult.Errors?.Any() == true)
            {
            }
            await phaseRepo.AddOrUpdate(phasesResult.AllPhases);

            // TODO: both trainingday and phasestatistics need previous data as well (e.g. trained second half of training day in another session
            // TODO: don't optimize early, re-run aggregation from scratch each time for now
            var trainingDays = TrainingDayAccount.Create(userId, 0, phasesResult.AllPhases);
            await repos.TrainingDays.AddOrUpdate(trainingDays);

            var phaseStats = PhaseStatistics.Create(0, phasesResult.AllPhases);
            await repos.PhaseStatistics.AddOrUpdate(phaseStats);
        }
        //public async Task UpdateAggregates(Session session, List<LogItem> logItems, string userId)
        //{
        //    // TODO: Move to async service (Azure (Durable) Functions maybe)?
        //    // But if we want to adapt response depending on some analysis... Then it's easier to handle it right here (instead of waiting for separate service)

        //    if (session.UserRepositories == null)
        //    {
        //        var tableFactory = new TableClientFactory();
        //        await tableFactory.Init();
        //        session.UserRepositories = new UserGeneratedRepositories(tableFactory, userId);
        //    }

        //    var phaseRepo = session.UserRepositories.Phases;
        //    var phasesResult = LogEventsToPhases.Create(logItems, await phaseRepo.GetAll());
        //    if (phasesResult.Errors?.Any() == true)
        //    {
        //    }
        //    await phaseRepo.AddOrUpdate(phasesResult.AllPhases);

        //    // TODO: both trainingday and phasestatistics need previous data as well (e.g. trained second half of training day in another session
        //    // TODO: don't optimize early, re-run aggregation from scratch each time for now
        //    var trainingDays = TrainingDayAccount.Create(userId, 0, phasesResult.AllPhases);
        //    await session.UserRepositories.TrainingDays.AddOrUpdate(trainingDays);

        //    var phaseStats = PhaseStatistics.Create(0, phasesResult.AllPhases);
        //    await session.UserRepositories.PhaseStatistics.AddOrUpdate(phaseStats);
        //}
    }
}
