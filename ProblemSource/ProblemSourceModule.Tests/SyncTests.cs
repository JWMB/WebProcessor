using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PluginModuleBase;
using ProblemSource.Models;
using ProblemSource.Models.LogItems;
using ProblemSource.Services;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Services.Storage;
using Shouldly;

namespace ProblemSource.Tests
{
    public class SyncTests
    {
        private readonly IFixture fixture;
        private readonly IDataSink dataSink;
        //private readonly IUserStateRepository userStateRepository;
        private readonly IBatchRepository<UserGeneratedState> userStateRepository;
        private readonly ProblemSourceProcessingMiddleware pipeline;

        public SyncTests()
        {
            fixture = new Fixture().Customize(new AutoMoqCustomization() { ConfigureMembers = true });

            dataSink = fixture.Create<IDataSink>();

            var mockUserStateRepo = new Mock<IBatchRepository<UserGeneratedState>>();
            userStateRepository = mockUserStateRepo.Object;

            //var mockRepoProvider = new Mock<IUserGeneratedDataRepositoryProvider>();
            //mockRepoProvider.Setup(o => o.UserStates).Returns(mockUserStateRepo.Object);

            //var mockRepoProviderFactory = new Mock<IUserGeneratedDataRepositoryProviderFactory>();
            //mockRepoProviderFactory.Setup(o => o.Create(It.IsAny<int>())).Returns(mockRepoProvider.Object);

            var mockTrainingRepo = new Mock<ITrainingRepository>();
            mockTrainingRepo.Setup(o => o.Get(It.IsAny<int>())).ReturnsAsync(() => new Training { TrainingPlanName = "2017 HT template Default" });

            var mockClientSessionManager = new Mock<IClientSessionManager>();
            mockClientSessionManager.Setup(o => o.GetOrOpenSession(It.IsAny<string>(), It.IsAny<string?>())).Returns(new GetOrCreateSessionResult(new Session("")));

            pipeline = new ProblemSourceProcessingMiddleware(new EmbeddedTrainingPlanRepository(),
                mockClientSessionManager.Object, dataSink, fixture.Create<IEventDispatcher>(), fixture.Create<IAggregationService>(),
                TestHelpers.MockDataRepositoryProviderFactory(userStateRepo: userStateRepository),
                new UsernameHashing(new MnemoJapanese(2), 2), new MnemoJapanese(2),
                mockTrainingRepo.Object,
                fixture.Create<ILogger<ProblemSourceProcessingMiddleware>>());
        }

        [Fact]
        public async Task Sync_LogsToDataSinkAndUserStateRepo()
        {
            // Arrange

            var userStatePushLogItem = new UserStatePushLogItem
            {
                className = nameof(UserStatePushLogItem),
                exercise_stats = new ExerciseStats { device = fixture.Create<DeviceInfo>() },
                user_data = new { }
            };

            var originalEvents = new List<object> {
                    userStatePushLogItem,
                    new LogItem { className = "unknown" }
            };
            var input = new SyncInput
            {
                Uuid = "musa tadube",
                Events = originalEvents.ToArray()
            };

            // Act
            var _ = await pipeline.Sync(input);

            // Assert
            Mock.Get(userStateRepository).Verify(x =>
                x.Upsert(It.Is<IEnumerable<UserGeneratedState>>(o => o.Single().exercise_stats.device.uuid == userStatePushLogItem.exercise_stats.device.uuid)), Times.Once);

            //DataSink: the UserStatePushLogItem entry is removed
            Mock.Get(dataSink).Verify(x =>
                x.Log(It.IsAny<string>(), It.Is<object>(o => ((SyncInput)o).Events.Count() == originalEvents.Count() - 1)), Times.Once);
        }

        [Fact]
        public async Task Sync_RequestState_ReturnsTrainingPlan()
        {
            // Arrange
            var input = new SyncInput
            {
                Uuid = "musa tadube",
                Events = new object[0],
                RequestState = true,
            };

            // Act
            var result = await pipeline.Sync(input);

            // Assert
            var state = JsonConvert.DeserializeObject<UserFullState>(result.state);
            if (state == null) throw new NullReferenceException(nameof(state));

            var trainingPlan = (JObject)state.training_plan;
            var metaphor = trainingPlan["metaphor"];
            if (metaphor == null) throw new NullReferenceException();
            
            metaphor.Value<string>().ShouldBe("Magical");
            state.training_settings.customData?.unlockAllPlanets.ShouldBe(false);
        }
    }
}