using AutoFixture;
using AutoFixture.AutoMoq;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PluginModuleBase;
using ProblemSource.Models;
using ProblemSource.Models.LogItems;
using ProblemSource.Services;
using Shouldly;

namespace ProblemSource.Tests
{
    public class SyncTests
    {
        private readonly IFixture fixture;
        private readonly IDataSink dataSink;
        private readonly IUserStateRepository userStateRepository;
        private readonly ProblemSourceProcessingPipeline pipeline;

        public SyncTests()
        {
            fixture = new Fixture().Customize(new AutoMoqCustomization() { ConfigureMembers = true });

            dataSink = fixture.Create<IDataSink>();
            userStateRepository = fixture.Create<IUserStateRepository>();
            pipeline = new ProblemSourceProcessingPipeline(userStateRepository, new TrainingPlanRepository(),
                fixture.Create<IClientSessionManager>(), dataSink, fixture.Create<IEventDispatcher>(), fixture.Create<IAggregationService>());
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

            var input = new SyncInput
            {
                Uuid = fixture.Create<string>(),
                Events = new List<object> {
                    userStatePushLogItem,
                    new LogItem { className = "unknown" }
                }.ToArray()
            };

            // Act
            var _ = await pipeline.Sync(JsonConvert.SerializeObject(input));

            // Assert
            Mock.Get(userStateRepository).Verify(x =>
                x.Set(It.IsAny<string>(), It.Is<object>(o => JObject.Parse(JsonConvert.SerializeObject(o))["exercise_stats"]["device"]["uuid"].Value<string?>() == userStatePushLogItem.exercise_stats.device.uuid)), Times.Once);

            //DataSink: the UserStatePushLogItem entry is removed
            Mock.Get(dataSink).Verify(x =>
                x.Log(It.IsAny<string>(), It.Is<object>(o => (o as SyncInput).Events.Count() == input.Events.Count() - 1)), Times.Once);
        }

        [Fact]
        public async Task Sync_RequestState_ReturnsTrainingPlan()
        {
            // Arrange
            var input = new SyncInput
            {
                Uuid = fixture.Create<string>(),
                Events = new object[0],
                RequestState = true,
            };

            // Act
            var result = await pipeline.Sync(JsonConvert.SerializeObject(input));

            // Assert
            var state = JsonConvert.DeserializeObject<UserFullState>(result.state);
            state!.training_plan.metaphor.ShouldBe("Magical");
            state!.training_settings.customData.unlockAllPlanets.ShouldBe(true);
        }
    }
}