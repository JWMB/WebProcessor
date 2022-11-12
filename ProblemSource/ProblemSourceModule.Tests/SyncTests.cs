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
        private readonly ProblemSourceProcessingPipeline pipeline;

        public SyncTests()
        {
            fixture = new Fixture().Customize(new AutoMoqCustomization() { ConfigureMembers = true });

            dataSink = fixture.Create<IDataSink>();
            userStateRepository = fixture.Create<IUserStateRepository>();
            pipeline = new ProblemSourceProcessingPipeline(userStateRepository, new TrainingPlanRepository(),
                fixture.Create<IClientSessionManager>(), dataSink, fixture.Create<IEventDispatcher>(), fixture.Create<IAggregationService>(), 
                fixture.Create<AzureTableUserGeneratedDataRepositoriesProviderFactory>(), new UsernameHashing(new MnemoJapanese(2), 2), new MnemoJapanese(2),
                fixture.Create<ILogger<ProblemSourceProcessingPipeline>>());
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
            //TODO: state!.training_plan.metaphor.ShouldBe("Magical");
            state!.training_settings.customData?.unlockAllPlanets.ShouldBe(true);
        }

        [Fact]
        public async Task TableClient_StorePhase()
        {
            var userId = "_test";
            //var phaseData = """{ "id":0,"training_day":3,"exercise":"tangram01#0","phase_type":"TEST","time":1666182070947,"sequence":0,"problems":[{ "id":0,"phase_id":0,"level":1.5,"time":1666182072961,"problem_type":"ProblemTangram","problem_string":"triangles","answers":[]}],"user_test":{ "score":0,"target_score":3,"planet_target_score":3,"won_race":false,"completed_planet":false,"ended":true}}""";
            //var phase = JsonConvert.DeserializeObject<Phase>(phaseData);
            //if (phase == null)
            //    throw new NullException("Deserializing phaseData");
            var phase = new Phase { id = 0, training_day = 3, exercise = "tangram01", phase_type = "TEST", time = 1666182070947, sequence = 0, problems = new List<Problem>(), user_test = new UserTest() };

            var clientFactory = new TableClientFactory(null);
            await clientFactory.Init();

            var tableEntity = PhaseTableEntity.FromBusinessObject(phase!, userId);
            try
            {
                var response = await clientFactory.Phases.UpsertEntityAsync(tableEntity);
                response.Status.ShouldBe(204); //409 (Conflict)
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                throw new Exception("Running old version of Azurite? This was fixed in 3.19.0 https://github.com/Azure/Azurite/issues/1565");
            }

            //var repo = new TableEntityRepository<Phase, PhaseTableEntity>(clientFactory.Phases, p => p.ToBusinessObject(), p => PhaseTableEntity.FromBusinessObject(p, userId), userId);
            //await repo.AddOrUpdate(new[] { phase });
        }
    }
}