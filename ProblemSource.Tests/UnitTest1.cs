using AutoFixture;
using AutoFixture.AutoMoq;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PluginModuleBase;
using ProblemSource.Models;
using ProblemSource.Models.Statistics;
using ProblemSource.Services;
using Shouldly;

namespace ProblemSource.Tests
{
    public class UnitTest1
    {
        private readonly IFixture fixture;
        private readonly IDataSink dataSink;
        private readonly IUserStateRepository userStateRepository;
        private readonly ProblemSourceProcessingPipeline pipeline;

        public UnitTest1()
        {
            fixture = new Fixture().Customize(new AutoMoqCustomization() { ConfigureMembers = true });

            dataSink = fixture.Create<IDataSink>();
            userStateRepository = fixture.Create<IUserStateRepository>();
            pipeline = new ProblemSourceProcessingPipeline(userStateRepository, new TrainingPlanRepository(),
                fixture.Create<IClientSessionManager>(), dataSink, fixture.Create<IEventDispatcher>());
        }

        [Fact]
        public async Task Test1()
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
                ApiKey = "abc",
                Uuid = fixture.Create<string>(),
                Events = new List<object> {
                    //new UserStatePushLogItem { className = nameof(UserStatePushLogItem) },
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
        public async Task Test2()
        {
            // Arrange
            var input = new SyncInput
            {
                ApiKey = "abc",
                Uuid = fixture.Create<string>(),
                Events = new object[0],
                RequestState = true,
            };

            // Act
            var result = await pipeline.Sync(JsonConvert.SerializeObject(input));

            var state = JsonConvert.DeserializeObject<UserFullState>(result.state);

            state.training_plan.metaphor.ShouldBe("Magical");
            state.training_settings.customData.unlockAllPlanets.ShouldBe(true);
        }

        [Fact]
        public async Task Sync()
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
                };
            PrepareLog(logItems);

            var input = new SyncInput
            {
                ApiKey = "abc",
                Uuid = fixture.Create<string>(),
                Events = logItems.ToArray(),
                RequestState = true,
            };

            var sessionManager = new InMemorySessionManager();
            var pipeline = new ProblemSourceProcessingPipeline(fixture.Create<IUserStateRepository>(), new TrainingPlanRepository(),
                sessionManager, fixture.Create<IDataSink>(), fixture.Create<IEventDispatcher>());

            var syncResult = await pipeline.Sync(JsonConvert.SerializeObject(input));

            var phaseRepo = sessionManager.GetOrOpenSession(input.Uuid, syncResult.sessionToken).Session.UserRepositories!.Phases;
            (await phaseRepo.GetAll()).Count().ShouldBe(2);
            //Mock.Get<IUserRepositories>().Verify
            //Mock.Get<IRepository<Phase>>().Verify(o => o.AddOrUpdate(It.IsAny<IEnumerable<Phase>>)).
        }

        [Fact]
        public void X()
        {
            var logItems = new List<LogItem>
            {
                new NewPhaseLogItem { time = 1, exercise = "A" },
                new NewProblemLogItem { time = 2 },
                new AnswerLogItem { time = 3 }
            };
            PrepareLog(logItems);

            var phasesResult = LogEventsToPhases.Create(logItems);

            phasesResult.PhasesCreated.Count.ShouldBe(1);
            phasesResult.Errors.ShouldBeEmpty();

            logItems = new List<LogItem> {
                new SyncLogStateLogItem { type = "ALREADY_SYNCED" },
            }.Concat(logItems)
            .Concat(new List<LogItem> {
                new SyncLogStateLogItem { type = "NOT_SYNCED" },
                new NewProblemLogItem { time = 6 },
                new AnswerLogItem { time = 7 },
                new PhaseEndLogItem { time = 8 },
                new NewPhaseLogItem { time = 9, exercise = "B" },
                new NewProblemLogItem { time = 10 },
                new AnswerLogItem { time = 11 },
                new PhaseEndLogItem { time = 12 },
            }).ToList();

            var newPhasesResult = LogEventsToPhases.Create(logItems, phasesResult.PhasesCreated);
            newPhasesResult.PhasesUpdated.Count.ShouldBe(1);
            newPhasesResult.PhasesCreated.Count.ShouldBe(1);
        }

        private void PrepareLog(IEnumerable<LogItem> logItems)
        {
            foreach (var item in logItems)
            {
                item.className = item.GetType().Name;
            }
        }
    }
}