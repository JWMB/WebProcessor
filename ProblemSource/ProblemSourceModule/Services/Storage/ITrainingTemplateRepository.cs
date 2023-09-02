using ProblemSource.Models;
using ProblemSourceModule.Models;

namespace ProblemSourceModule.Services.Storage
{
    public interface ITrainingTemplateRepository
    {
        Task<Training?> Get(int id);
        Task<Training?> Get(string name);
        Task<IEnumerable<Training>> GetAll();
    }

    public class StaticTrainingTemplateRepository : ITrainingTemplateRepository
    {
        public async Task<Training?> Get(int id) => (await GetAll()).FirstOrDefault(o => o.Id == id);
        public async Task<Training?> Get(string name) => (await GetAll()).FirstOrDefault(o => o.Username == name);

        public Task<IEnumerable<Training>> GetAll()
        {
            // TODO: use real storage, move to service
            var templates = new[] {
                new Training { Id = 1, Username = "template_Default training", TrainingPlanName = "2017 HT template Default", Settings = CreateSettings(s =>
                {
                    s.Analyzers = new List<string> { nameof(TrainingAnalyzers.CategorizerDay5_23Q1) };
                }) },
                new Training { Id = 2, Username = "template_Test training", TrainingPlanName = "2023 VT template JonasTest", Settings = CreateSettings(s =>
                {
                    s.Analyzers = new List<string> { nameof(TrainingAnalyzers.ExperimentalAnalyzer) };
                    s.timeLimits = new List<decimal> { 3 };
                    s.customData = new CustomData { allowMultipleLogins = true };
                }) },
                new Training { Id = 3, Username = "template_NumberlineTest training", TrainingPlanName = "2023 VT template JonasTest", Settings = CreateSettings(s => {
                    s.customData = new CustomData {
                        allowMultipleLogins = true,
                        numberLine = new { changeSuccess = 0.4, changeFail = -1 } //skillChangeGood = 0.5 }
                    };
                    // TODO: Response serialization doesn't work (probably .NET built-in JSON vs dynamic/JToken)
                    s.UpdateTrainingOverrides(new[]{ TrainingSettings.CreateWeightChangeTrigger(new Dictionary<string, int> { {"Math", 100}, { "numberline", 100 } }, 0, 0) });
                }) },
                new Training { Id = 4, Username = "template_Test unlocked", TrainingPlanName = "DebugPlan", Settings = CreateSettings(s =>
                {
                    s.timeLimits = new List<decimal> { 30 };
                    s.customData = new CustomData { allowMultipleLogins = true, unlockAllPlanets = true, canEnterCompleted = true };
                }) },
                new Training { Id = 5, Username = "template_2023HT", TrainingPlanName = "2023 HT template Preview", Settings = CreateSettings(s =>
                {
                    s.timeLimits = new List<decimal> { 30 };
                    s.customData = new CustomData { };
                    s.Analyzers = new List<string> { nameof(TrainingAnalyzers.CategorizerDay5_23Q1) };
                }) },
            };

            return Task.FromResult((IEnumerable<Training>)templates);

            TrainingSettings CreateSettings(Action<TrainingSettings>? act = null)
            {
                var ts = TrainingSettings.Default;
                act?.Invoke(ts);
                return ts;
            }
        }
    }
}
