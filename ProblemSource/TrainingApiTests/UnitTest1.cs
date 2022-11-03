using Microsoft.EntityFrameworkCore;
using OldDb.Models;
using ProblemSource.Models;
using ProblemSource.Models.Aggregates;
using ProblemSource.Services;
using Shouldly;
using TrainingApi.Controllers;
using TrainingApi.Services;

namespace TrainingApiTests
{
    public class UnitTest1
    {
        [Fact]
        public async Task OldDbTraining_RecreateFromLogAndVerify()
        {
            using var db = new TrainingDbContext(new DbContextOptions<TrainingDbContext>() { });
            var accountId = 715955;
            var account = await db.Accounts.SingleOrDefaultAsync(o => o.Id == accountId);
            var phases = await RecreateLogFromOldDb.Get(db, 715955);
            var logItems = RecreateLogFromOldDb.ToLogItems(phases);

            var recreatedPhasesResult = LogEventsToPhases.Create(logItems, null);
            recreatedPhasesResult.Errors.ShouldBeEmpty();
            recreatedPhasesResult.PhasesCreated.Count.ShouldBe(phases.Count);

            var trainingDays = TrainingDayAccount.Create("", 0, recreatedPhasesResult.PhasesCreated); // await phaseRepo.GetAll());

            var oldTrainingDayRows = (await db.AggregatedData
                .Where(o => o.AggregatorId == 2 && o.AccountId == accountId)
                .OrderBy(o => o.OtherId)
                .ToListAsync())
                .OfType<TrainingDayAccount>().ToList()
;
            var oldTrainingDays = oldTrainingDayRows
                //.Select(o => o.ToTyped())
                .ToList();

            var oldSingleTrainingDays = oldTrainingDays
                .GroupBy(o => o.TrainingDay).Select(o => o.Last())
                .ToList();

            var joined = trainingDays.Join(oldSingleTrainingDays, o => o.TrainingDay, o => o.TrainingDay, (o, old) => new { New = o, Old = old }).ToList();

            var diff = joined.Where(o =>
            {
                return o.New == null || o.Old == null
                    //|| o.New.StartTime != o.Old.StartTime
                    || o.New.NumQuestions != o.Old.NumQuestions
                    || o.New.NumRacesWon != o.Old.NumRacesWon
                    || o.New.ResponseMinutes != o.Old.ResponseMinutes
                ;
            });
            diff.ShouldBeEmpty();
        }
    }
}