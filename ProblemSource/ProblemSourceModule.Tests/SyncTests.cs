using AutoFixture;
using AutoFixture.AutoMoq;
using Azure;
using Azure.Data.Tables;
using Castle.Core.Logging;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PluginModuleBase;
using ProblemSource.Models;
using ProblemSource.Models.Aggregates;
using ProblemSource.Models.LogItems;
using ProblemSource.Services;
using ProblemSource.Services.Storage;
using ProblemSource.Services.Storage.AzureTables;
using ProblemSource.Services.Storage.AzureTables.TableEntities;
using ProblemSourceModule.Services.Storage;
using Shouldly;
using System;
using Xunit.Sdk;

namespace ProblemSource.Tests
{
    public class SyncTests
    {
        private readonly IFixture fixture;
        private readonly IDataSink dataSink;
        private readonly IUserStateRepository userStateRepository;
        private readonly ProblemSourceProcessingMiddleware pipeline;

        public SyncTests()
        {
            fixture = new Fixture().Customize(new AutoMoqCustomization() { ConfigureMembers = true });

            dataSink = fixture.Create<IDataSink>();
            userStateRepository = fixture.Create<IUserStateRepository>();

            var mockTrainingRepo = new Mock<ITrainingRepository>();
            mockTrainingRepo.Setup(o => o.Get(It.IsAny<int>())).ReturnsAsync(() => new Training { TrainingPlanName = "2017 HT template Default" });

            pipeline = new ProblemSourceProcessingMiddleware(userStateRepository, new EmbeddedTrainingPlanRepository(),
                fixture.Create<IClientSessionManager>(), dataSink, fixture.Create<IEventDispatcher>(), fixture.Create<IAggregationService>(), 
                fixture.Create<AzureTableUserGeneratedDataRepositoriesProviderFactory>(), new UsernameHashing(new MnemoJapanese(2), 2), new MnemoJapanese(2),
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

#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.
            Mock.Get(userStateRepository).Verify(x =>
                x.Set(It.IsAny<string>(), It.Is<object>(o => JObject.Parse(JsonConvert.SerializeObject(o))["exercise_stats"]["device"]["uuid"].Value<string?>() == userStatePushLogItem.exercise_stats.device.uuid)), Times.Once);
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8602 // Dereference of a possibly null reference.

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

            var ooo = ((JObject)state!.training_plan)["metaphor"];
            if (ooo == null) throw new NullReferenceException();
            ooo.Value<string>().ShouldBe("Magical");
            state!.training_settings.customData?.unlockAllPlanets.ShouldBe(false);
        }
    }
}