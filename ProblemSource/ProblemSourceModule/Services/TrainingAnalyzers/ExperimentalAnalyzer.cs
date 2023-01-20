using Newtonsoft.Json;
using ProblemSource.Models;
using ProblemSource.Models.LogItems;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Models;

namespace ProblemSourceModule.Services.TrainingAnalyzers
{
    public class ExperimentalAnalyzer : ITrainingAnalyzer
    {
        public Task<bool> Analyze(Training training, List<LogItem> latestLogItems, IUserGeneratedDataRepositoryProvider provider)
        {
            var eod = latestLogItems.OfType<EndOfDayLogItem>().FirstOrDefault();

            if (eod != null)
            {
                var mathTest = """
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

                var weightChange = """
{
    "triggerTime": "MAP",
    "criteriaValues": [
    {
        "name": "trainingDay",
        "value": "{trainingDay}"
    }
    ],
    "actionData": {
        "type": "TrainingPlanModTriggerAction",
        "id": "modDay0_{trainingDay}",
        "properties": {
            "weights": {
                "Math": 40,
                "WM": 30,
                "Reasoning": 30,
                "tangram#intro": 100,
                "tangram": 100,
                "nvr_so": 100,
                "rotation#intro": 100,
                "rotation": 100,
                "nvr_rp": 100
            },
            "phases": {
                "^WM_[\\w#]+": { "medalMode": "ONE_WIN" },
                "^addsub[\\w#]*": { "lvlMgr": { "maxFallFromHighest": 5 } }
            }
        }
    }
}
""";
                var overrides = new
                {
                    triggers = new[] {
                        JsonConvert.DeserializeObject<dynamic>(weightChange.Replace("{trainingDay}", eod.training_day.ToString()))
                    }
                };
                training.Settings.trainingPlanOverrides = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(overrides));
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
    }
}
