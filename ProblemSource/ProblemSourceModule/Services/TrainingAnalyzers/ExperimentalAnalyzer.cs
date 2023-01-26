using Newtonsoft.Json;
using ProblemSource.Models;
using ProblemSource.Models.LogItems;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Models;

namespace ProblemSourceModule.Services.TrainingAnalyzers
{
    public class ExperimentalAnalyzer : ITrainingAnalyzer
    {
        public Task<bool> Analyze(Training training, IUserGeneratedDataRepositoryProvider provider, List<LogItem>? latestLogItems)
        {
            var eod = latestLogItems?.OfType<EndOfDayLogItem>().FirstOrDefault();

            if (eod != null)
            {
                var weightChange = CreateWeightChangeTrigger(new Dictionary<string, int> { });
                weightChange.actionData.id = $"modDay0_{eod.training_day}";
                var overrides = new
                {
                    triggers = new[] {
                        weightChange
                        //JsonConvert.DeserializeObject<dynamic>(weightChange.Replace("{trainingDay}", eod.training_day.ToString()))
                    }
                };
                training.Settings.trainingPlanOverrides = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(overrides));
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
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

        private dynamic CreateWeightChangeTrigger(Dictionary<string, int> weights)
        {

            var weigthsDefault = new Dictionary<string, int> {
                { "Math", 100 },
                { "WM", 100 },
                { "Reasoning", 100 },
                { "tangram#intro", 100 },
                { "tangram", 100 },
                { "nvr_so", 100 },
                { "rotation#intro", 100 },
                { "rotation", 100 },
                { "nvr_rp", 100 },
            };
            var def = """
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
            },
            "phases": {
                "^WM_[\\w#]+": { "medalMode": "ONE_WIN" },
                "^addsub[\\w#]*": { "lvlMgr": { "maxFallFromHighest": 5 } }
            }
        }
    }
}
""";
            var obj = JsonConvert.DeserializeObject<dynamic>(def);
            if (obj == null) throw new Exception("aaa");
            obj.actionData.properties.weigths = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(weigthsDefault));

            return obj;
        }
    }
}
