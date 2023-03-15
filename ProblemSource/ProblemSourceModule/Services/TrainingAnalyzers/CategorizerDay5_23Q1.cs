using ProblemSource.Models;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Models;
using Microsoft.Extensions.Logging;
using ProblemSource.Models.Aggregates;
using ML.Helpers;
using ML.Dynamic;
using Newtonsoft.Json;
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

            var dayJustCompleted = await ITrainingAnalyzer.WasDayJustCompleted(training, provider, latestLogItems, logStr => log.LogInformation(logStr));

            log.LogInformation($"Should run for {training.Id}? dayJustCompleted '{dayJustCompleted}' == {runAfterDay}?");
            if (runAfterDay == dayJustCompleted)
            {
                log.LogInformation($"Running prediction for training {training.Id}");
                var result = new PredictedNumberlineLevel { Predicted = null };
                try
                {
                    // TODO: how do we know if this has already been run? Doesn't really matter right now, but might for other types of analyzers.
                    result = await Predict(training, provider);
                    log.LogInformation($"Predicted performance for training {training.Id}: {result.Predicted}/{result.PredictedPerformanceTier}");
                }
                catch (Exception ex)
                {
                    log.LogError(ex, $"Error predicting for training {training.Id}");
                }

                if (result.PredictedPerformanceTier == PredictedNumberlineLevel.PerformanceTier.Unknown)
                    return false;

                var seedSrc = DateTime.Now;
                var rnd = new Random(seedSrc.Millisecond * 1000 + seedSrc.Microsecond);

                var trigger = CreateTrigger(runAfterDay + 1, result.PredictedPerformanceTier, (rnd.NextDouble(), rnd.NextDouble()));
                if (trigger != null)
                {
                    log.LogInformation($"ML-generated trigger added for training {training.Id}");
                    training.Settings.UpdateTrainingOverrides(new[] { trigger });
                    return true;
                }
            }
            log.LogInformation($"No training modifications for {training.Id}");
            return false;
        }

        public async Task<PredictedNumberlineLevel> Predict(Training training, IUserGeneratedDataRepositoryProvider provider)
        {
            var mlFeatures = await CreateFeatures(training, provider);
            var result = await modelService.Predict(mlFeatures);
            if (result.PredictedPerformanceTier == PredictedNumberlineLevel.PerformanceTier.Unknown)
            {
                log.LogWarning($"Could not predict performance for training {training.Id}: IsValid={mlFeatures.IsValid} Reasons={string.Join(",", mlFeatures.InvalidReasons.Select(o => $"{o.Key}:{o.Value}"))}");
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

    public class MLPredictNumberlineLevelService : IPredictNumberlineLevelService
    {
        private readonly IMLPredictor predictor;

        public MLPredictNumberlineLevelService(IMLPredictor predictor)
        {
            this.predictor = predictor;
        }

        public async Task<PredictedNumberlineLevel> Predict(IMLFeature feature)
        {
            if (!feature.IsValid)
                return new PredictedNumberlineLevel { Predicted = null };

            var colInfo = ColumnInfo.Create(feature.GetType());
            var flatFeatures = feature.GetFlatFeatures();

            return new PredictedNumberlineLevel { Predicted = await predictor.Predict(colInfo, flatFeatures) };
        }
    }

    public class RemoteMLPredictor : IMLPredictor
    {
        private readonly string endpoint;
        private readonly IHttpClientFactory httpClientFactory;

        public RemoteMLPredictor(string endpoint, IHttpClientFactory httpClientFactory)
        {
            this.endpoint = endpoint;
            this.httpClientFactory = httpClientFactory;
        }

        public async Task<float?> Predict(ColumnInfo colInfo, Dictionary<string, object?> parameters)
        {
            var client = httpClientFactory.CreateClient();

            var body = new
            {
                ColumnInfo = colInfo,
                Parameters = parameters
            };

            var response = await client.PostAsync(endpoint, new StringContent(JsonConvert.SerializeObject(body), new System.Net.Http.Headers.MediaTypeHeaderValue("application/json")));

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Predict endpoint response: {(int)response.StatusCode}/{response.ReasonPhrase}");

            var result = await response.Content.ReadAsStringAsync();
            if (result == null)
                throw new Exception($"Predict response empty");

            if (float.TryParse(result, out var value))
                return value;

            var json = JsonConvert.DeserializeObject<JObject>(result);
            var valuex = json?["Predicted"]?.Value<float?>();

            return valuex;
        }
    }

    public class LocalMLPredictor : IMLPredictor
    {
        private readonly string localModelPath;

        public LocalMLPredictor(string localModelPath)
        {
            this.localModelPath = localModelPath;
        }
        public Task<float?> Predict(ColumnInfo colInfo, Dictionary<string, object?> parameters)
        {
            var prediction = MLDynamicPredict.PredictFromModel(localModelPath, colInfo, parameters);
            return Task.FromResult((float?)prediction);
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
