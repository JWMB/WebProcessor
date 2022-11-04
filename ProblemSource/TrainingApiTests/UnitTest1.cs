using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OldDb.Models;
using ProblemSource.Models;
using ProblemSource.Models.Aggregates;
using ProblemSource.Services;
using Shouldly;
using TrainingApi.Services;

namespace TrainingApiTests
{
    public class UnitTest1
    {
        public UnitTest1()
        {
            Skip.If(!System.Diagnostics.Debugger.IsAttached);
        }

        [SkippableFact]
        public async Task OldDbTraining_DeserializeAccounts()
        {
            using var db = new TrainingDbContext(new DbContextOptions<TrainingDbContext>() { });

            var schoolAccountsGroups = await db.AccountsGroups
                .Include(o => o.Group)
                .Where(o => o.Group != null && o.Group.Name != null && o.Group.Name.StartsWith("_school"))
                .ToListAsync();
            var dict = schoolAccountsGroups.GroupBy(o => o.Group.Name).Where(o => o.Count() > 100).ToDictionary(o => o.Key, o => o.ToList());

            var info = new List<string>();
            // 236944 
            foreach (var accountId in dict.First().Value.Select(o => o.AccountId))
            {
                OldDb.Models.Account account;
                try
                {
                    account = await db.Accounts
                        .Include(o => o.ClientLogs)
                        .Include(o => o.UsageLogs)
                        //.Include(o => o.SyncLogs) // Too heavy - many rows with lots of data in each
                        .Where(o => o.Id == accountId).SingleAsync();
                }
                catch (Exception ex)
                {
                    info.Add($"{accountId} {ex.Message}");
                    continue;
                }

                try
                {
                    if (account.ExerciseStats == null || account.UserData == null)
                    {
                        if (account.ExerciseStats != null || account.UserData != null)
                        {
                            info.Add($"One null, not other? {account.ExerciseStats == null}");
                        }
                    }
                    else
                    {
                        var userState = new UserGeneratedState
                        {
                            exercise_stats = JsonConvert.DeserializeObject<ExerciseStats>(account.ExerciseStats),
                            user_data = JToken.Parse(account.UserData),
                        };
                    }
                    var trainingSettings = string.IsNullOrEmpty(account.TrainingSettings) ? null : JsonConvert.DeserializeObject<TrainingSettings>(account.TrainingSettings);
                }
                catch (Exception ex)
                {
                    info.Add($"{accountId}: {ex.Message} {ex.GetType().Name} empty? ud:{string.IsNullOrEmpty(account.UserData)} xs:{string.IsNullOrEmpty(account.ExerciseStats)} ts:{string.IsNullOrEmpty(account.TrainingSettings)}");
                }

                if (account.ClientLogs.Any())
                { }
                if (account.UsageLogs.Any())
                { }
            }

            return;

            //account.UserData.ShouldBe("");
            //account.ExerciseStats.ShouldBe("");
            //account.TrainingSettings.ShouldBe("");

            //account.ClientLogs.Count().ShouldBe(0);
            //account.UsageLogs.ShouldBeEmpty();
            //account.SyncLogs.Count().ShouldBe(0);
        }

        [SkippableFact]
        public async Task OldDbTraining_RecreateFromLogAndVerify()
        {
            using var db = new TrainingDbContext(new DbContextOptions<TrainingDbContext>() { });
            var accountId = 715955;
            var account = await db.Accounts.SingleOrDefaultAsync(o => o.Id == accountId);
            var phases = await RecreateLogFromOldDb.GetFullPhases(db, 715955);
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