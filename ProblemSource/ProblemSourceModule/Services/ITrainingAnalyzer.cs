using ProblemSource.Models.Aggregates;
using ProblemSource.Models;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Models;
using ProblemSource.Models.LogItems;
using Microsoft.Extensions.Logging;

namespace ProblemSourceModule.Services
{
    public class TrainingAnalyzerCollection
    {
        private readonly IEnumerable<ITrainingAnalyzer> instances;
        private readonly ILogger<TrainingAnalyzerCollection> log;

        public TrainingAnalyzerCollection(IEnumerable<ITrainingAnalyzer> instances, ILogger<TrainingAnalyzerCollection> log)
        {
            this.instances = instances;
            this.log = log;
        }

        public async Task<bool> Execute(Training training, List<LogItem> latestLogItems, IUserGeneratedDataRepositoryProvider provider)
        {
            if (instances?.Any() != true)
                return false;

            var modified = false;
            foreach (var item in instances)
            {
                try
                {
                    modified |= await item.Analyze(training, latestLogItems, provider);
                }
                catch (Exception ex)
                {
                    log.LogWarning($"{nameof(MLFeaturesJulia)}: {ex.Message}", ex);
                    return false;
                }
            }
            return modified;
        }
    }

    public interface ITrainingAnalyzer
    {
        Task<bool> Analyze(Training training, List<LogItem> latestLogItems, IUserGeneratedDataRepositoryProvider provider);
    }


    public interface IJuliaModelService
    {
        Task<JuliaModelResult> Post(MLFeaturesJulia features);
    }
    public class NullJuliaModelService : IJuliaModelService
    {
        public async Task<JuliaModelResult> Post(MLFeaturesJulia features)
        {
            await Task.Delay(1000);

            return new JuliaModelResult { IsLowestQuartile = false };
            //var response = await client.PostAsJsonAsync("", features.ToArray());
            //return await response.Content.ReadFromJsonAsync<JuliaModelResult>();
        }
    }

    public class JuliaModelResult
    {
        public bool IsLowestQuartile { get; set; }
    }

    public class JuliaMLCategorizer : ITrainingAnalyzer
    {
        private readonly IJuliaModelService modelService;

        public JuliaMLCategorizer(IJuliaModelService modelService)
        {
            this.modelService = modelService;
        }

        public async Task<bool> Analyze(Training training, List<LogItem> latestLogItems, IUserGeneratedDataRepositoryProvider provider)
        {
            var eod = latestLogItems.OfType<EndOfDayLogItem>().FirstOrDefault();
            // TODO: also check if stat's TrainingDay has changed

            if (eod?.training_day == 5)
            {
                var age = 6; // TODO: where can we get age? Add in TrainingSettings for now?
                var mlFeatures = MLFeaturesJulia.FromPhases(training.Settings ?? new TrainingSettings(), await provider.Phases.GetAll(), age: age);
                var result = await modelService.Post(mlFeatures);
                if (training.Settings == null)
                {
                    training.Settings = new TrainingSettings();
                }

                if (result.IsLowestQuartile)
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
}
