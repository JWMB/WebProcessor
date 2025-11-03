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
using Newtonsoft.Json;
using ProblemSource.Models;
using ML.Helpers;
using ProblemSource.Tests;

namespace ProblemSourceModule.Tests
{
    public class TrainingAnalyzerTests
    {
        private readonly IFixture fixture;
        public TrainingAnalyzerTests()
        {
            fixture = new Fixture().Customize(new AutoMoqCustomization() { ConfigureMembers = true });
        }

        [Theory]
        [InlineData(typeof(TrainingModCreator_23Q1))]
        [InlineData(typeof(TrainingModCreator_24Q1))]
        public void CategorizerDay5_Triggers(Type creatorType)
        {
            ITrainingModCreator trainingModsCreator = (ITrainingModCreator)Activator.CreateInstance(creatorType)!; // new CreateTrainingMods_23Q1();
            var trainingDay = 0; // doesn't matter for this test
            // expected WM Weights
            var wmWeights = new
            {
                NVR_Std = "38",
                NVR_High = "20",
                WM_Std = "46"
            };

            foreach (var rnd in new[] { 0.0, 0.5, 1.0 })
            {
                foreach (var tier in new[] { PredictedNumberlineLevel.PerformanceTier.Low, PredictedNumberlineLevel.PerformanceTier.Medium, PredictedNumberlineLevel.PerformanceTier.High } )
                {
                    var expectedWMWeight = trainingModsCreator is TrainingModCreator_24Q1
                        ? rnd switch
                        {
                            0.0 => wmWeights.NVR_Std,
                            0.5 => wmWeights.NVR_High,
                            _ => wmWeights.WM_Std
                        } // the rest is fdr the old TrainingModCreator_23Q1
                        : tier == PredictedNumberlineLevel.PerformanceTier.Low
                            ? rnd switch
                            {
                                0.0 => wmWeights.NVR_Std,
                                0.5 => wmWeights.NVR_High,
                                _ => wmWeights.NVR_High,
                            }
                            : (tier == PredictedNumberlineLevel.PerformanceTier.Medium
                                ? wmWeights.NVR_Std
                                // PerformanceTier.High
                                : rnd switch
                                {
                                    0.0 => wmWeights.NVR_Std,
                                    0.5 => wmWeights.WM_Std,
                                    _ => wmWeights.WM_Std,
                                });
                    var trigger = trainingModsCreator.CreateTrigger(trainingDay, tier, (rnd, 0));
                    $"{trigger!.actionData.properties.weights.WM}".ShouldBe(expectedWMWeight);
                }
            }

            var low00 = trainingModsCreator.CreateTrigger(5, PredictedNumberlineLevel.PerformanceTier.Low, (0, 0));
            $"{low00!.actionData.properties.weights.WM}".ShouldBe("38");
            $"{low00!.actionData.properties.phases}".ShouldContain("numberline[\\\\w#]*");

            var low11 = trainingModsCreator.CreateTrigger(5, PredictedNumberlineLevel.PerformanceTier.Low, (1, 1));
            $"{low11!.actionData.properties.weights.WM}".ShouldBe(trainingModsCreator is TrainingModCreator_23Q1 ? "20" : "46");
            $"{low11!.actionData.properties.phases}".ShouldNotContain("numberline[\\\\w#]*");

            var medium = trainingModsCreator.CreateTrigger(5, PredictedNumberlineLevel.PerformanceTier.Medium, (0, 0));
            $"{medium!.actionData.properties.weights.WM}".ShouldBe("38"); // (medium as object).ShouldBeNull();

            var unknown = trainingModsCreator.CreateTrigger(5, PredictedNumberlineLevel.PerformanceTier.Unknown, (0, 0));
            $"{unknown!.actionData.properties.weights.WM}".ShouldBe("38"); //(unknown as object).ShouldBeNull();

            var high0 = trainingModsCreator.CreateTrigger(5, PredictedNumberlineLevel.PerformanceTier.High, (0, 0));
            $"{high0!.actionData.properties.weights.WM}".ShouldBe("38");

            var high1 = trainingModsCreator.CreateTrigger(5, PredictedNumberlineLevel.PerformanceTier.High, (1, 0));
            $"{high1!.actionData.properties.weights.WM}".ShouldBe("46");
        }


        [Theory]
        [InlineData(10, true)]
        [InlineData(50, true)] // medium prediction should now also have changes
        [InlineData(100, true)]
        public async Task CategorizerDay5_23Q1_Modified(float predictedLevel, bool expectedModification)
        {
            var predicter = new Mock<IPredictNumberlineLevelService>();
            predicter.Setup(o => o.Predict(It.IsAny<IMLFeature>())).Returns(Task.FromResult(new PredictedNumberlineLevel { Predicted = predictedLevel }));
            var i = new CategorizerDay5_23Q1(predicter.Object, fixture.Create<ILogger<CategorizerDay5_23Q1>>());
            var modified = await i.Analyze(new Training(), fixture.Create<IUserGeneratedDataRepositoryProvider>(), new List<LogItem> { new EndOfDayLogItem { training_day = 5 } });

            modified.ShouldBe(expectedModification);
            //var result = await i.Predict(new Training(), fixture.Create<IUserGeneratedDataRepositoryProvider>());
            //result.Predicted.ShouldBe(fixedPredictedValue);
        }

        [Fact]
        public void ModifyTrigger()
        {
            var trigger = TrainingSettings.CreateWeightChangeTrigger(new Dictionary<string, int> { { "WM", 1 } }, 5);
            trigger.actionData.properties.phases = TrainingSettings.ConvertToDynamicOrThrow(new Dictionary<string, object> {
                    {
                        "numberline[\\w#]*",
                        new { problemGeneratorData = new { problemFile = new { path = "blabla.csv" } } }
                    }
            });

            var serialized = (string)JsonConvert.SerializeObject(trigger);
            serialized.ShouldContain("""phases":{"numberline[\\w#]*":{"problemGeneratorData""");
        }

        [Fact]
        public async Task ExperimentalAnalyzer_TriggeredOnEndOfDayLogItem()
        {
            // Arrange
            var trainingDay = 5;
            var analyzer = new ExperimentalAnalyzer();
            var training = new Training { };
            var logItems = new List<LogItem> { new EndOfDayLogItem { training_day = trainingDay } };

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
                var logItems = new List<LogItem> { new EndOfDayLogItem { training_day = i } };
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

            var training = new Training { Settings = new TrainingSettings { 
                Analyzers = isEnabled ? analyzers.Select(o => o.GetType().Name).ToList() : new List<string>()
            }};

            // Act
            var result = await collection.Execute(training, fixture.Create<IUserGeneratedDataRepositoryProvider>(), new List<LogItem> { new EndOfDayLogItem { training_day = 1 } }.ToList());

            // Assert
            result.ShouldBe(isEnabled);
        }

        [Fact]
        public async Task Analyzer_TriggeredOnEndOfDayLogItem()
        {
            // Arrange
            var trainingDay = 5;
            var logItems = new List<LogItem> { new EndOfDayLogItem { training_day = trainingDay} };
            var training = new Training();

            var justCompletedDay = await ITrainingAnalyzer.WasDayJustCompleted(training, fixture.Create<IUserGeneratedDataRepositoryProvider>(), logItems);

            justCompletedDay.ShouldBe(trainingDay);
        }


        [Fact]
        public async Task WasDayJustCompleted_Logging()
        {
            // Stupid test, but something's not working with WasDayJustCompleted logging...
            var trainingDay = 5;
            var logItems = new List<LogItem> { new EndOfDayLogItem { training_day = trainingDay } };

            var logs = new List<string>();
            var justCompletedDay = await ITrainingAnalyzer.WasDayJustCompleted(new Training(), fixture.Create<IUserGeneratedDataRepositoryProvider>(), logItems, logStr => logs.Add(logStr));

            logs.Count.ShouldBe(1);
        }

        [Fact]
        public async Task WasDayJustCompleted_Logging_From_CategorizerDay5_23Q1()
        {
            // Stupid test, but something's not working with WasDayJustCompleted logging...
            var trainingDay = 5;
            var logItems = new List<LogItem> { new EndOfDayLogItem { training_day = trainingDay } };

            // https://codeburst.io/unit-testing-with-net-core-ilogger-t-e8c16c503a80#0b05
            var logs = new InMemoryLogger<CategorizerDay5_23Q1>();

            var analyzer = new CategorizerDay5_23Q1(new NullPredictNumberlineLevelService(), logs);
            await analyzer.Analyze(new Training(), new Mock<IUserGeneratedDataRepositoryProvider>().Object, logItems);

            logs.LogItems.Count.ShouldBe(4);
            logs.LogItems.Count(o => o.Item2.Contains("WasDayJustCompleted")).ShouldBe(1);
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

        [Fact]
        public async Task RemoteMLPredictor_Integration()
        {
            var feature = new MLFeaturesJulia
            {
                Id = 1,

                ActiveTimePerDay = 10,
                Age = 6,
                Age6_7 = true,
                MeanTimeIncrease = 2,
                TrainingTime20Min = true,
                ByExercise = new Dictionary<string, MLFeaturesJulia.FeaturesForExercise>
                {
                    { "wm_grid", new MLFeaturesJulia.FeaturesForExercise { FractionCorrect = 0.5M, HighestLevelInt = 10, NumProblems = 20 } },
                    { "numberline", new MLFeaturesJulia.FeaturesForExercise { FractionCorrect = 0.5M, HighestLevelInt = 10, NumProblems = 20 } },
                    { "npals", new MLFeaturesJulia.FeaturesForExercise { FractionCorrect = 0.5M, HighestLevelInt = 10, NumProblems = 20 } },
                    { "nvr_so", new MLFeaturesJulia.FeaturesForExercise { FractionCorrect = 0.5M, MedianLevel = 5 } },
                    { "nvr_rp", new MLFeaturesJulia.FeaturesForExercise { FractionCorrect = 0.5M, MedianLevel = 5 } }
                },

                FinalNumberLineLevel = null,
            };

            if (feature.InvalidReasons.Any())
                throw new Exception($"Invalid features: {string.Join(",", feature.InvalidReasons)}");

            var colInfo = ColumnInfo.Create(feature.GetType());
            var flatFeatures = feature.GetFlatFeatures();

            var config = TestHelpers.CreateConfig();
            var url = config["MLPredictUrl"];
            var predictor = new RemoteMLPredictor(url!, new TestHttpClientFactory());

            var result = await predictor.Predict(colInfo, flatFeatures);

            result.ShouldNotBeNull();
        }

        [Fact]
        public async Task AiCoachAnalyzer_Test()
        {
            var sut = new AiCoachAnalyzer();
            var training = new Training { Settings = new TrainingSettings { timeLimits = [33] }, AgeBracket = "4-5" };
			var repoProvider = new Mock<IUserGeneratedDataRepositoryProvider>();

            var summary = new TrainingSummary { TrainedDays = 5 };
			var trainingSummaries = new Mock<IBatchRepository<TrainingSummary>>();
			trainingSummaries.Setup(o => o.GetAll()).ReturnsAsync(new List<TrainingSummary> { summary });
			repoProvider.Setup(o => o.TrainingSummaries).Returns(trainingSummaries.Object);

			var trainingDaySummaries = new Mock<IBatchRepository<TrainingDayAccount>>();
			var trainingDays = Enumerable.Range(1, summary.TrainedDays).Select(day => new TrainingDayAccount
			{
				TrainingDay = day,
				RemainingMinutes = 10,
				ResponseMinutes = 20,
                StartTime = DateTime.UtcNow.AddDays(-day),
				EndTimeStamp = DateTime.UtcNow.AddDays(-day).AddMinutes(30)
			});
			trainingDaySummaries.Setup(o => o.GetAll()).ReturnsAsync(trainingDays.ToList());
			repoProvider.Setup(o => o.TrainingDays).Returns(trainingDaySummaries.Object);
			repoProvider.Setup(o => o.Phases).Returns(new Mock<IBatchRepository<Phase>>().Object);

			var exercises = new[] { "WM_grid#intro", "WM_grid", "nvr_so" };
			var phaseStatistics = new Mock<IBatchRepository<PhaseStatistics>>();
			phaseStatistics.Setup(o => o.GetAll()).ReturnsAsync(CreatePhaseStatistics(exercises, 3, summary.TrainedDays));
			repoProvider.Setup(o => o.PhaseStatistics).Returns(phaseStatistics.Object);

            var bracketProviders = new[] { (training, repoProvider.Object) };

			var prompt = await sut.CreatePrompt(training, repoProvider.Object, bracketProviders);

			List<PhaseStatistics> CreatePhaseStatistics(IEnumerable<string> exercises, decimal level, int toTrainingDay)
			{
                return exercises.SelectMany(exercise => 
                    Enumerable.Range(1, toTrainingDay).Select(day => new PhaseStatistics { exercise = exercise, training_day = day, level_max = level }))
                    .ToList();
				//return new List<PhaseStatistics>
    //            {
    //                new PhaseStatistics { exercise = "WM_grid#intro", level_max = level },
				//	new PhaseStatistics { exercise = "WM_grid", level_max = level }
				//};
			}
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

        public class TestHttpClientFactory : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => new HttpClient();
        }

        public class InMemoryLogger<T> : ILogger<T>
        {
            public List<(LogLevel, string, Exception?)> LogItems { get; set; } = new();
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                LogItems.Add((logLevel, $"{state}", exception));
            }
        }
    }
}
