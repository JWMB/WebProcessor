using AutoFixture;
using Newtonsoft.Json;
using PluginModuleBase;
using ProblemSource.Models.LogItems;
using ProblemSource.Models;
using ProblemSource.Services;
using AutoFixture.AutoMoq;
using Shouldly;
using ProblemSource.Services.Storage;

namespace ProblemSource.Tests
{
    public class AggregationServiceTests
    {
        private readonly IFixture fixture;
        public AggregationServiceTests()
        {
            fixture = new Fixture().Customize(new AutoMoqCustomization() { ConfigureMembers = true });
        }

        [Fact]
        public async Task Sync_AggregatesUpdated()
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

            var userId = fixture.Create<string>();

            var tableFactory = new TableClientFactory();
            await tableFactory.Init();
            var userRepos = new UserGeneratedRepositories(tableFactory, userId);

            var aggS = new AggregationService();
            await aggS.UpdateAggregates(userRepos, logItems, userId);

            (await userRepos.Phases.GetAll()).Count().ShouldBe(2);
        }
    }
}
