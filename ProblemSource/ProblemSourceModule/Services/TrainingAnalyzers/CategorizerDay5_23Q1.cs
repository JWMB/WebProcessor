using ProblemSource.Models;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Models;
using Common;
using Microsoft.Extensions.Logging;
using ProblemSource.Models.Aggregates;
using ML.Helpers;
using ML.Dynamic;
using Newtonsoft.Json.Linq;

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

        //public Func<Training, IUserGeneratedDataRepositoryProvider, Task<IMLFeature>> FuncCreateFeatures { get; set; } = CreateFeatures
        private static async Task<IMLFeature> CreateFeatures(Training training, IUserGeneratedDataRepositoryProvider provider)
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
                // TODO: how do we know if this has already been run? Doesn't really matter right now, but might for other types of analyzers.
                var result = await Predict(training, provider);

                if (result.PredictedPerformanceTier == PredictedNumberlineLevel.PerformanceTier.Unknown)
                    return false;

                log.LogInformation($"Predicted performance for training {training.Id}: {result.Predicted}/{result.PredictedPerformanceTier}");

                var seedSrc = DateTime.Now;
                var rnd = new Random(seedSrc.Millisecond * 1000 + seedSrc.Microsecond);

                var trigger = CreateTrigger(runAfterDay + 1, result.PredictedPerformanceTier, (rnd.NextDouble(), rnd.NextDouble()));
                if (trigger != null)
                {
                    training.Settings.UpdateTrainingOverrides(new[] { trigger });
                    return true;
                }
            }
            return false;
        }

        public async Task<PredictedNumberlineLevel> Predict(Training training, IUserGeneratedDataRepositoryProvider provider)
        {
            var mlFeatures = await CreateFeatures(training, provider);
            var result = await modelService.Predict(mlFeatures);
            if (result.PredictedPerformanceTier == PredictedNumberlineLevel.PerformanceTier.Unknown)
            {
                log.LogWarning($"Could not predict performance for training {training.Id}: IsValid={mlFeatures.IsValid}");
            }
            return result;
        }

        public static dynamic? CreateTrigger(int triggerDay, PredictedNumberlineLevel.PerformanceTier tier, (double, double) rnds)
        {
            // TK: only nvr_so - rotation, tangram and nvr_rp can be removed from day 6.
            var plans = new
            {
                NVR_Std = new Dictionary<string, int> { { "Math", 50 }, { "WM", 38 }, { "NVR", 12 }, { "tangram", 33 }, { "nvr_so", 66 }, { "nvr_rp", 0 }, { "rotation", 0 } }, //{ "NVR", 8 }, { "tangram", 4 } },
                WM_Std = new Dictionary<string, int> { { "Math", 50 }, { "WM", 46 }, { "NVR", 4 }, { "tangram", 100 }, { "nvr_so", 0 }, { "nvr_rp", 0 }, { "rotation", 0 } }, //{ "NVR", 0 }, { "tangram", 4 } },
                NVR_High = new Dictionary<string, int> { { "Math", 50 }, { "WM", 20 }, { "NVR", 30 }, { "tangram", 13 }, { "nvr_so", 87 }, { "nvr_rp", 0 }, { "rotation", 0 } }, // { "NVR", 26 }, { "tangram", 4 } },
            };

            //"tangram": 100, "tangram#intro": 100,
            //"rotation": 0, "rotation#intro": 0

            if (tier == PredictedNumberlineLevel.PerformanceTier.Low)
            {
                var trigger = TrainingSettings.CreateWeightChangeTrigger(
                    rnds.Item1 < 0.5
                    ? plans.NVR_Std
                    : plans.NVR_High, triggerDay);

                if (rnds.Item2 < 0.5)
                {
                    trigger.actionData.properties.phases = TrainingSettings.ConvertToDynamicOrThrow(new Dictionary<string, object> {
                        {
                            "numberline[\\w#]*",
                            new { problemGeneratorData = new { problemFile = new { path = "numberline_easy_ola_q123.csv" } } } // Note: client updated to include this file
                        } });
                }
                return trigger;
            }
            else if (tier == PredictedNumberlineLevel.PerformanceTier.High)
            {
                // Randomize WM vs NVR
                return TrainingSettings.CreateWeightChangeTrigger(
                    rnds.Item1 < 0.5
                    ? plans.NVR_Std
                    : plans.WM_Std, triggerDay);
            }
            else
            {
                // Standard NVR - no change
            }

            return null;
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

            var colInfo = ColumnInfo.Create(feature.GetType());
            var flatFeatures = feature.GetFlatFeatures();

            var prediction = MLDynamicPredict.PredictFromModel(localModelPath, colInfo, flatFeatures);
            return (float?)prediction;
            //return 35;
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
                    <= 36f => PerformanceTier.Low, // TODO: set limits
                    <= 87f => PerformanceTier.Medium,
                    _ => PerformanceTier.High,
                };
            }
        }

        public float? Predicted { get; set; }
    }
}
