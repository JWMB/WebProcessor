using AutoFixture;
using ProblemSource.Models.LogItems;
using ProblemSource.Models;
using ProblemSource.Services;
using AutoFixture.AutoMoq;
using Shouldly;
using ProblemSource.Services.Storage;
using Moq;
using ProblemSource.Models.Aggregates;
using ProblemSource.Services.Storage.AzureTables;
using Microsoft.Extensions.Logging;

namespace ProblemSource.Tests
{
    public class AggregationServiceTests
    {
        private readonly IFixture fixture;

        public AggregationServiceTests()
        {
            fixture = new Fixture().Customize(new AutoMoqCustomization() { ConfigureMembers = true });
        }

        private void EnableNonDebugSkip() => Skip.If(!System.Diagnostics.Debugger.IsAttached);

        [SkippableFact]
        public async Task Aggregates_IndividualAggregators()
        {
            EnableNonDebugSkip();
            var uuid = "test name";
            var id = 1;

            var phases = Enumerable.Range(0, 10).Select(pi => Phase.CreateForTest(pi));

            var tableFactory = new TypedTableClientFactory(null);
            await tableFactory.Init();
            var userRepos = new AzureTableUserGeneratedDataRepositoryProvider(tableFactory, id);

            await userRepos.Phases.AddOrUpdate(phases);

            var phaseStats = PhaseStatistics.Create(0, phases);
            await userRepos.PhaseStatistics.AddOrUpdate(phaseStats);

            var trainingDays = TrainingDayAccount.Create(id, await userRepos.PhaseStatistics.GetAll());
            await userRepos.TrainingDays.AddOrUpdate(trainingDays);
        }

        [SkippableFact]
        public async Task InvalidAzureKeyCharactersHandled()
        {
            EnableNonDebugSkip();

            var userId = 1;
            var phases = new[] {
                new Phase { exercise = "a#" },
                //new Phase { exercise = "a?" } // TODO
            };

            var tableFactory = new TypedTableClientFactory(null);
            await tableFactory.Init();
            var userRepos = new AzureTableUserGeneratedDataRepositoryProvider(tableFactory, userId);

            await Should.NotThrowAsync(async () => await userRepos.Phases.AddOrUpdate(phases));
        }

        [SkippableFact]
        public async Task AggregatesUpdated_Table()
        {
            EnableNonDebugSkip();

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

            var userId = fixture.Create<int>();

            var tableFactory = new TypedTableClientFactory(null);
            await tableFactory.Init();
            var userRepos = new AzureTableUserGeneratedDataRepositoryProvider(tableFactory, userId);

            var aggS = new AggregationService(fixture.Create<ILogger<AggregationService>>());
            await aggS.UpdateAggregates(userRepos, logItems, userId);

            (await userRepos.Phases.GetAll()).Count().ShouldBe(2);
        }

        [Fact]
        public async Task AggregatesUpdated_Mock()
        {
            // Arrange
            var logItems = new List<LogItem> {
                    new SyncLogStateLogItem { type = "NOT_SYNCED" },
                    new NewPhaseLogItem { time = 5, exercise = "A" },
                    new NewProblemLogItem { time = 6 },
                    new AnswerLogItem { time = 7 },
                    new PhaseEndLogItem { time = 8 },
                    new NewPhaseLogItem { time = 9, exercise = "B" },
                    new NewProblemLogItem { time = 10 },
                    new AnswerLogItem { time = 11 },
                    new PhaseEndLogItem { time = 12 },
                }.Prepare().ToList();

            var userId = fixture.Create<int>();

            var repoProvider = fixture.Create<IUserGeneratedDataRepositoryProvider>();
            Mock.Get(repoProvider).Setup(o => o.Phases).Returns(new InMemoryBatchRepository<Phase>(Phase.UniqueIdWithinUser));

            var aggS = new AggregationService(fixture.Create<ILogger<AggregationService>>());
            await aggS.UpdateAggregates(repoProvider, logItems, userId);

            (await repoProvider.Phases.GetAll()).Count().ShouldBe(2);
        }
    }
}
