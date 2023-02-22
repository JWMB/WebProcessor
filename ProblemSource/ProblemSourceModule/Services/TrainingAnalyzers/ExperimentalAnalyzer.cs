using Newtonsoft.Json;
using ProblemSource.Models;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Models;

namespace ProblemSourceModule.Services.TrainingAnalyzers
{
    public class ExperimentalAnalyzer : ITrainingAnalyzer
    {
        public async Task<bool> Analyze(Training training, IUserGeneratedDataRepositoryProvider provider, List<LogItem>? latestLogItems)
        {
            var justFinishedDay = await ITrainingAnalyzer.WasDayJustCompleted(training, provider, latestLogItems);
            if (justFinishedDay == null)
                return false;

            var trainingDay = justFinishedDay.Value;
            var trainingDayForChange = trainingDay + 1;

            //var groupNames = new[] { "Math", "WM", "Reasoning" };
            //var groupWeigths = groupNames.Select((o, i) => new { Key = o, Value = i == (trainingDay % groupNames.Length) ? 100 : 0 }).ToDictionary(o => o.Key, o => o.Value);
            var groupWeigths = new Dictionary<string, int> { { "WM", 0 }, { "NVR", 0 }, { "Reasoning", 0 }, { "Math", 100 }, { "npals", 0 } };
            var trigger = TrainingSettings.CreateWeightChangeTrigger(groupWeigths, trainingDayForChange);
            trigger.actionData.properties.phases = TrainingSettings.ConvertToDynamicOrThrow(new Dictionary<string, object> {
            {
                "numberline[\\w#]*",
                new { problemGeneratorData = new { problemFile = new { path = "numberline_jonastest.csv" } } }
            } });
            var overrides = new
            {
                triggers = new[] { trigger }
            };

            training.Settings.trainingPlanOverrides = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(overrides));
            return true;
        }

        private string MathTest()
        {
            return """
{
    "triggerTime": "MAP",
    "criteriaValues": [
    {
        "name": "trainingDay",
        "value": "{trainingDay}"
    }
    ],
    "actionData": {
        "type": "StartExerciseTriggerAction",
        "id": "eval_mathtest_day{trainingDay}",
        "properties": {
            "id": "mathTest01",
            "progVisualizer": "ProgVisualizerSnails"
        }
    }
}
""";
        }
    }
}
