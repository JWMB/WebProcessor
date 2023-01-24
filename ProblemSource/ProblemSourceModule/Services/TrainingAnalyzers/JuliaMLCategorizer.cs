using ProblemSource.Models.Aggregates;
using ProblemSource.Models;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Models;
using ProblemSource.Models.LogItems;

namespace ProblemSourceModule.Services.TrainingAnalyzers
{
    public class JuliaMLCategorizer : ITrainingAnalyzer
    {
        private readonly IJuliaModelService modelService;

        public JuliaMLCategorizer(IJuliaModelService modelService)
        {
            this.modelService = modelService;
        }

        public async Task<bool> Analyze(Training training, IUserGeneratedDataRepositoryProvider provider, List<LogItem>? latestLogItems)
        {
            var runAfterDay = 5;

            if (await ITrainingAnalyzer.WasDayJustCompleted(runAfterDay, provider, latestLogItems))
            {
                var age = 6; // TODO: where can we get age? Add in TrainingSettings for now?
                var mlFeatures = MLFeaturesJulia.FromPhases(training.Settings ?? new TrainingSettings(), await provider.Phases.GetAll(), age: age);
                var result = await modelService.Post(mlFeatures);
                training.Settings ??= TrainingSettings.Default;

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
}
