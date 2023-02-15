using AutoFixture.AutoMoq;
using AutoFixture;
using ProblemSourceModule.Models;
using ProblemSourceModule.Services.TrainingAnalyzers;
using ProblemSource.Services.Storage;
using Shouldly;
using ProblemSource.Models.LogItems;
using Newtonsoft.Json.Linq;
using Moq;
using ProblemSourceModule.Models.Aggregates;
using ProblemSourceModule.Services;
using Microsoft.Extensions.Logging;
using ProblemSource.Models.Aggregates;

namespace ProblemSourceModule.Tests
{
    public class TrainingAnalyzerTests
    {
        private readonly IFixture fixture;
        public TrainingAnalyzerTests()
        {
            fixture = new Fixture().Customize(new AutoMoqCustomization() { ConfigureMembers = true });
        }

        [Fact]
        public async Task ExperimentalAnalyzer_TriggeredOnEndOfDayLogItem()
        {
            // Arrange
            var trainingDay = 5;
            var analyzer = new ExperimentalAnalyzer();
            var training = new Training { };
            var logItems = new List<ProblemSource.Models.LogItem> { new EndOfDayLogItem { training_day = trainingDay } };

            // Act
            var modified = await analyzer.Analyze(training, fixture.Create<IUserGeneratedDataRepositoryProvider>(), logItems);

            // Assert
            modified.ShouldBeTrue();

            training.Settings.trainingPlanOverrides.ShouldNotBeNull();
            var overrides = (JObject)training.Settings.trainingPlanOverrides;

            overrides["triggers"]?[0]?["actionData"]?["type"]?.ToString().ShouldBe("TrainingPlanModTriggerAction");
            overrides["triggers"]?[0]?["actionData"]?["id"]?.ToString().ShouldBe($"modDay0_{trainingDay + 1}");
        }

        [Fact]
        public async Task ExperimentalAnalyzer_DifferentWeightPerDay()
        {
            // Arrange
            var analyzer = new ExperimentalAnalyzer();
            var training = new Training { };

            for (int i = 0; i < 3; i++)
            {
                var logItems = new List<ProblemSource.Models.LogItem> { new EndOfDayLogItem { training_day = i } };
                // Act
                var modified = await analyzer.Analyze(training, fixture.Create<IUserGeneratedDataRepositoryProvider>(), logItems);

                // Assert
                modified.ShouldBeTrue();

                training.Settings.trainingPlanOverrides.ShouldNotBeNull();
                var overrides = (JObject)training.Settings.trainingPlanOverrides;
                var weights = overrides["triggers"]?[0]?["actionData"]?["properties"]?["weights"];

                // TODO: Assert in code
                //weights?["Math"]?.Value<int>().ShouldBe(100);
                //"Math": 100, "WM": 0, "Reasoning": 100,
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Analyzer_TriggeredOnlyIfEnabledInSettings(bool isEnabled)
        {
            // Arrange
            var analyzers = new ITrainingAnalyzer[] { new ExperimentalAnalyzer() };
            var collection = new TrainingAnalyzerCollection(analyzers, fixture.Create<ILogger<TrainingAnalyzerCollection>>());

            var training = new Training { Settings = new ProblemSource.Models.TrainingSettings { 
                Analyzers = isEnabled ? analyzers.Select(o => o.GetType().Name).ToList() : new List<string>()
            }};

            // Act
            var result = await collection.Execute(training, fixture.Create<IUserGeneratedDataRepositoryProvider>(), new List<ProblemSource.Models.LogItem> { new EndOfDayLogItem { training_day = 1 } }.ToList());

            // Assert
            result.ShouldBe(isEnabled);
        }

        [Fact]
        public async Task Analyzer_TriggeredOnEndOfDayLogItem()
        {
            // Arrange
            var trainingDay = 5;
            var logItems = new List<ProblemSource.Models.LogItem> { new EndOfDayLogItem { training_day = trainingDay} };
            var training = new Training();

            var justCompletedDay = await ITrainingAnalyzer.WasDayJustCompleted(training, fixture.Create<IUserGeneratedDataRepositoryProvider>(), logItems);

            justCompletedDay.ShouldBe(trainingDay);
        }

        [Theory]
        [InlineData(false, 2, true)]
        [InlineData(false, 0, false)]
        [InlineData(true, 0, true)]
        public async Task Analyzer_TriggeredOnTrainingSummaries(bool trainedFullMinutes, int hoursSinceSync, bool expectedCompleted)
        {
            // Arrange
            var trainingDay = 5;
            var training = new Training();

            var summary = new TrainingSummary { TrainedDays = trainingDay, LastLogin = DateTimeOffset.UtcNow.AddHours(-hoursSinceSync) };
            var repoProvider = MockIUserGeneratedDataRepositoryProvider(summary, trainedFullMinutes ? (int)training.Settings.timeLimits.First() : 0);

            // Act
            var justCompletedDay = await ITrainingAnalyzer.WasDayJustCompleted(training, repoProvider, null);

            // Assert
            if (expectedCompleted) justCompletedDay.ShouldBe(trainingDay);
            else justCompletedDay.ShouldBeNull();
        }

        private IUserGeneratedDataRepositoryProvider MockIUserGeneratedDataRepositoryProvider(TrainingSummary summary, int totalMinutesTrained = 33)
        {
            var repoProvider = new Mock<IUserGeneratedDataRepositoryProvider>();

            var trainingSummaries = new Mock<IBatchRepository<TrainingSummary>>();
            trainingSummaries.Setup(o => o.GetAll()).ReturnsAsync(new List<TrainingSummary> { summary });
            repoProvider.Setup(o => o.TrainingSummaries).Returns(trainingSummaries.Object);

            var trainingDaySummaries = new Mock<IBatchRepository<TrainingDayAccount>>();
            var trainingDay = new TrainingDayAccount {
                TrainingDay = summary.TrainedDays,
                RemainingMinutes = 0,
                ResponseMinutes = totalMinutesTrained,
                EndTimeStamp = summary.LastLogin.UtcDateTime
            };
            trainingDaySummaries.Setup(o => o.GetAll()).ReturnsAsync(new List<TrainingDayAccount> { trainingDay });
            repoProvider.Setup(o => o.TrainingDays).Returns(trainingDaySummaries.Object);

            return repoProvider.Object;
        }
    }
}
