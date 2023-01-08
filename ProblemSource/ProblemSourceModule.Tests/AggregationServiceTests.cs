using AutoFixture;
using ProblemSource.Models.LogItems;
using ProblemSource.Models;
using ProblemSource.Services;
using AutoFixture.AutoMoq;
using Shouldly;
using ProblemSource.Services.Storage;
using Moq;
using ProblemSource.Models.Aggregates;
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
           
            // Act
            await aggS.UpdateAggregates(repoProvider, logItems, userId);

            // Assert
            (await repoProvider.Phases.GetAll()).Count().ShouldBe(2);
        }
    }
}
