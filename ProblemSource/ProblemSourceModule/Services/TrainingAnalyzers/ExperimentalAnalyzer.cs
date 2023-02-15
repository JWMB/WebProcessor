using Newtonsoft.Json;
using ProblemSource.Models;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Models;
using System.Runtime.Serialization;

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

            var groupNames = new[] { "Math", "WM", "Reasoning" };
            var groupWeigths = groupNames.Select((o, i) => new { Key = o, Value = i == (trainingDay % groupNames.Length) ? 100 : 0 }).ToArray();

            var weightChange = CreateWeightChangeTrigger(groupWeigths.ToDictionary(o => o.Key, o => o.Value), trainingDayForChange);
            var overrides = new
            {
                triggers = new[] { weightChange }
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

        public static void UpdateTrainingOverrides(Training training, IEnumerable<object> triggers, bool removePreExistingOverrides = true)
        {
            dynamic overrides;
            if (training.Settings.trainingPlanOverrides == null || removePreExistingOverrides)
            {
                overrides = new
                {
                    triggers = triggers.ToArray()
                };
            }
            else
            {
                overrides = training.Settings.trainingPlanOverrides;
                overrides.triggers = triggers.ToArray();
            }

            training.Settings.trainingPlanOverrides = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(overrides));
        }

        public static dynamic CreateTrigger(int trainingDay)
        {
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
            }
        }
    }
}
""".Replace("{trainingDay}", trainingDay.ToString());

            //"phases": {
            //    "^WM_[\\w#]+": { "medalMode": "ONE_WIN" },
            //    "^addsub[\\w#]*": { "lvlMgr": { "maxFallFromHighest": 5 } }
            //}

            var obj = JsonConvert.DeserializeObject<dynamic>(def);
            if (obj == null)
                throw new Exception("Couldn't deserialize trigger");
            return obj;
        }

        public static dynamic CreateWeightChangeTrigger(Dictionary<string, int> weights, int trainingDay)
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
            foreach (var key in weigthsDefault.Keys.Except(weights.Keys))
                weights[key] = weigthsDefault[key];

            var trigger = CreateTrigger(trainingDay);
            trigger.actionData.properties.weights = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(weights));

            return trigger;
        }
    }
}
