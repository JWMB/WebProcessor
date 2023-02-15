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
            return MLFeaturesJulia.FromPhases(training.Settings ?? new TrainingSettings(), await provider.Phases.GetAll(), age: age);
        }

        public async Task<bool> Analyze(Training training, IUserGeneratedDataRepositoryProvider provider, List<LogItem>? latestLogItems)
        {
            var runAfterDay = 5;

            if (runAfterDay == await ITrainingAnalyzer.WasDayJustCompleted(training, provider, latestLogItems))
            {
                var age = 6; // TODO: where can we get age? Add in TrainingSettings for now?

                var mlFeatures = await CreateFeatures(training, provider);
                var result = await modelService.Predict(mlFeatures);

                if (result.PredictedPerformanceTier == PredictedNumberlineLevel.PerformanceTier.Unknown)
                {
                    log.LogWarning($"Could not predict performance for training {training.Id}: IsValid={mlFeatures.IsValid}");
                    return false;
                }

                log.LogInformation($"Predicted performance for training {training.Id}: {result.Predicted}/{result.PredictedPerformanceTier}");

                training.Settings ??= TrainingSettings.Default;

                if (result.PredictedPerformanceTier == PredictedNumberlineLevel.PerformanceTier.Low)
                {
                    training.Settings.timeLimits = training.Settings.timeLimits.Select(o => o * 0.9M).ToList();
                    return true;
                }
                else
                {
                    training.Settings.timeLimits = training.Settings.timeLimits.Select(o => o * 1.1M).ToList();
                    //training.Settings.trainingPlanOverrides = new { AA = 12 };
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
        public Task<PredictedNumberlineLevel> Predict(IMLFeature features)
        {
            var predicted = CreatePrediction(features);
            return Task.FromResult(new PredictedNumberlineLevel { Predicted = 10 });
        }

        public float? CreatePrediction(IMLFeature feature)
        {
            if (!feature.IsValid)
                return null;

            ITransformer model;
            DataViewSchema schema;

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

        public PerformanceTier PredictedPerformanceTier => PerformanceTier.Low;
        public float? Predicted { get; set; }
    }
}
