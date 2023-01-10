using AutoFixture;
using Microsoft.Extensions.Logging;
using ProblemSource.Models;
using ProblemSource.Models.Aggregates;
using ProblemSource.Models.LogItems;
using ProblemSource.Services;
using ProblemSource.Services.Storage.AzureTables;
using ProblemSource.Tests;
using Shouldly;

namespace ProblemSourceModule.Tests.AzureTable
{
    public class AzureAggregatesTests : AzureTableTestBase
    {
        [SkippableFact]
        public async Task Aggregates_IndividualAggregators()
        {
            await Init(removeAllRows: true);

            var id = 1;

            var phases = Enumerable.Range(0, 10).Select(Phase.CreateForTest);

            var userRepos = new AzureTableUserGeneratedDataRepositoryProvider(tableClientFactory, id);

            await userRepos.Phases.Upsert(phases);

            var phaseStats = PhaseStatistics.Create(0, phases);
            await userRepos.PhaseStatistics.Upsert(phaseStats);

            var trainingDays = TrainingDayAccount.Create(id, await userRepos.PhaseStatistics.GetAll());
            await userRepos.TrainingDays.Upsert(trainingDays);
        }

        [SkippableFact]
        public async Task AggregatesUpdated_Table()
        {
            await Init(removeAllRows: true);

            // Arrange
            var logItems = new List<LogItem> {
                    new SyncLogStateLogItem { type = "NOT_SYNCED" },
                    new NewPhaseLogItem { time = 5, exercise = "A#1" },
                    new NewProblemLogItem { time = 6 },
                    new AnswerLogItem { time = 7 },
                    new PhaseEndLogItem { time = 8 },
                    new NewPhaseLogItem { time = 9, exercise = "B#2" },
                    new NewProblemLogItem { time = 10 },
                    new AnswerLogItem { time = 11 },
                    new PhaseEndLogItem { time = 12 },
                }.Prepare().ToList();

            var userId = 1;

            var userRepos = new AzureTableUserGeneratedDataRepositoryProvider(tableClientFactory, userId);
            // Pre-assert
            (await userRepos.Phases.GetAll()).Count().ShouldBe(0);

            var aggS = new AggregationService(fixture.Create<ILogger<AggregationService>>());

            // Act
            await aggS.UpdateAggregates(userRepos, logItems, userId);

            // Assert
            (await userRepos.Phases.GetAll()).Count().ShouldBe(2);

            var summaries = await userRepos.TrainingSummaries.GetAll();
            summaries.Count().ShouldBe(1);
        }
    }
}
