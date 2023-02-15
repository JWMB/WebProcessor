using ProblemSource.Models;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Models;
using ProblemSourceModule.Models.Aggregates.ML;
using Common;
using Microsoft.ML;
using Microsoft.Extensions.Logging;

namespace ProblemSourceModule.Services.TrainingAnalyzers
{
    public class CategorizerDay5_23Q1 : ITrainingAnalyzer
    {
        private readonly IPredictNumberlineLevelService modelService;
        private readonly ILogger<CategorizerDay5_23Q1> log;

        public CategorizerDay5_23Q1(IPredictNumberlineLevelService modelService, ILogger<CategorizerDay5_23Q1> log)
        {
            this.modelService = modelService;
            this.log = log;
        }

        private async Task<IMLFeature> CreateFeatures(Training training, IUserGeneratedDataRepositoryProvider provider)
        {
            if (!int.TryParse(training.AgeBracket.Split('-').Where(o => o.Any()).FirstOrDefault() ?? "6", out var age))
                age = 6;
            return MLFeaturesJulia.FromPhases(training.Settings ?? TrainingSettings.Default, await provider.Phases.GetAll(), age: age);
        }

        public async Task<bool> Analyze(Training training, IUserGeneratedDataRepositoryProvider provider, List<LogItem>? latestLogItems)
        {
            var runAfterDay = 5;
            training.Settings ??= TrainingSettings.Default;

            if (runAfterDay == await ITrainingAnalyzer.WasDayJustCompleted(training, provider, latestLogItems))
            {
                var mlFeatures = await CreateFeatures(training, provider);
                var result = await modelService.Predict(mlFeatures);

                if (result.PredictedPerformanceTier == PredictedNumberlineLevel.PerformanceTier.Unknown)
                {
                    log.LogWarning($"Could not predict performance for training {training.Id}: IsValid={mlFeatures.IsValid}");
                    return false;
                }
                log.LogInformation($"Predicted performance for training {training.Id}: {result.Predicted}/{result.PredictedPerformanceTier}");
                var seedSrc = DateTime.Now;
                var rnd = new Random(seedSrc.Millisecond * 1000 + seedSrc.Microsecond);

                var plans = new
                {
                    NVR_Std = new Dictionary<string, int> { { "Math", 50 }, { "WM", 38 }, { "NVR", 8 }, { "tangram", 4 } },
                    WM_Std = new Dictionary<string, int> { { "Math", 50 }, { "WM", 46 }, { "NVR", 0 }, { "tangram", 4 } },
                    NVR_High = new Dictionary<string, int> { { "Math", 50 }, { "WM", 20 }, { "NVR", 26 }, { "tangram", 4 } },
                };

                //TODO: weights within NVR? Rotation?
                //"tangram#intro": 100,
                //"tangram": 100,
                //"nvr_rp": 0,
                //"nvr_so": 0,
                //"rotation": 0,
                //"rotation#intro": 0

                var triggerDay = runAfterDay + 1;
                dynamic? trigger = null;

                if (result.PredictedPerformanceTier == PredictedNumberlineLevel.PerformanceTier.Low)
                {
                    trigger = ExperimentalAnalyzer.CreateWeightChangeTrigger(
                        rnd.NextDouble() < 0.5
                        ? plans.NVR_Std
                        : plans.NVR_High, triggerDay);

                    if (rnd.NextDouble() < 0.5)
                    {
                        trigger.actionData.properties.phases = new Dictionary<string, object> {
                        {
                            "numberline[\\w#]*",
                            new { problemGeneratorData = new { problemFile = new { path = "blabla.csv" } } } // TODO: will we be using this? If so specify!
                        } };
                    }
                }
                else if (result.PredictedPerformanceTier == PredictedNumberlineLevel.PerformanceTier.High)
                {
                    // Randomize WM vs NVR
                    trigger = ExperimentalAnalyzer.CreateWeightChangeTrigger(
                        rnd.NextDouble() < 0.5
                        ? plans.NVR_Std
                        : plans.WM_Std, triggerDay);
                }
                else
                {
                    // Standard NVR
                    return false;
                }

                if (trigger != null)
                {
                    ExperimentalAnalyzer.UpdateTrainingOverrides(training, new[] { trigger });
                    return true;
                }


            }
            return false;
        }
    }

    public interface IPredictNumberlineLevelService
    {
        Task<PredictedNumberlineLevel> Predict(IMLFeature features);
    }

    public class NullPredictNumberlineLevelService : IPredictNumberlineLevelService
    {
        public async Task<PredictedNumberlineLevel> Predict(IMLFeature features)
        {
            await Task.Delay(1000);
            return new PredictedNumberlineLevel { Predicted = 10 };
        }
    }

    public class LocalMLPredictNumberlineLevelService : IPredictNumberlineLevelService
    {
        private readonly string localModelPath;

        public LocalMLPredictNumberlineLevelService(string localModelPath)
        {
            this.localModelPath = localModelPath;
        }

        public Task<PredictedNumberlineLevel> Predict(IMLFeature features)
        {
            var predicted = CreatePrediction(features);
            return Task.FromResult(new PredictedNumberlineLevel { Predicted = predicted });
        }

        public float? CreatePrediction(IMLFeature feature)
        {
            if (!feature.IsValid)
                return null;

            if (!File.Exists(localModelPath))
                return null;

            var ctx = new MLContext(seed: 0);

            var model = ctx.Model.Load(localModelPath, out DataViewSchema schema);
            var colInfo = ColumnInfo.Create(feature.GetType());

            var type = MLDynamicPredict.CreateType(schema);
            var predictor = new MLDynamicPredict(schema, model, colInfo);
            var instance = DynamicTypeFactory.CreateInstance(type, feature.GetFlatFeatures());

            var prediction = predictor.Predict(instance);
            return (float?)prediction;
        }
    }

    public class PredictedNumberlineLevel
    {
        public enum PerformanceTier
        {
            Unknown,
            Low,
            Medium,
            High
        }

        public PerformanceTier PredictedPerformanceTier
        {
            get
            {
                return Predicted switch
                {
                    null => PerformanceTier.Unknown,
                    <= 0.2f => PerformanceTier.Low,
                    <= 0.8f => PerformanceTier.Medium,
                    _ => PerformanceTier.High,
                };
            }
        }

        public float? Predicted { get; set; }
    }
}
