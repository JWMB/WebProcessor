using Newtonsoft.Json;
using ProblemSource.Models;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Models;
using static ProblemSourceModule.Services.TrainingAnalyzers.CategorizerDay5_23Q1;

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

            //var groupWeigths = groupNames.Select((o, i) => new { Key = o, Value = i == (trainingDay % groupNames.Length) ? 100 : 0 }).ToDictionary(o => o.Key, o => o.Value);
            var groupWeigths = new Dictionary<string, int> { { GroupNames.WM, 0 }, { GroupNames.Reasoning, 0 }, { GroupNames.Math, 100 }, { "npals", 0 } };
            var trigger = TrainingSettings.CreateWeightChangeTrigger(groupWeigths, trainingDayForChange);
            trigger.actionData.properties.phases = TrainingSettings.ConvertToDynamicOrThrow(new Dictionary<string, object> {
            {
                "numberline[\\w#]*",
                new { problemGeneratorData = new { problemFile = new { path = "numberline_easy_ola_q123.csv" } } }
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

    public class ClientTestingAnalyzer : ITrainingAnalyzer
    {
        public async Task<bool> Analyze(Training training, IUserGeneratedDataRepositoryProvider provider, List<LogItem>? latestLogItems)
        {
            var justFinishedDay = await ITrainingAnalyzer.WasDayJustCompleted(training, provider, latestLogItems);
            if (justFinishedDay == null)
                return false;

            var trainingDay = justFinishedDay.Value;

            if (trainingDay == 5)
                Execute(training, trainingDay);

            return true;
        }

        private void Execute(Training training, int trainingDay)
        {
            var trainingDayForChange = trainingDay + 1;

            var groupWeights = new Dictionary<string, int> {
                { GroupNames.WM, 10 },

                { GroupNames.Reasoning, 10 },
                { "tangram", 33 }, { "nvr_so", 66 }, { "nvr_rp", 0 }, { "rotation", 0 }, { "boolean", 0 },
                
                { GroupNames.Math, 80 },
            };
            var trigger = TrainingSettings.CreateWeightChangeTrigger(groupWeights, trainingDayForChange);
            var overrides = new
            {
                triggers = new[] { trigger }
            };

            training.Settings.trainingPlanOverrides = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(overrides));
        }
    }
}
