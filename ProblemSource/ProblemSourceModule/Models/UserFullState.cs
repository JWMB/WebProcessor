using Newtonsoft.Json;
using ProblemSourceModule.Models;
using System.Text.Json;

namespace ProblemSource.Models
{
    public interface IUserServerSettings
    {
        string uuid { get; set; }
        object training_plan { get; set; }
        TrainingSettings training_settings { get; set; }
    }

    public class UserServerSettings : IUserServerSettings
    {
        public string uuid { get; set; } = "";
        public object training_plan { get; set; } = new(); //TrainingPlan
        public TrainingSettings training_settings { get; set; } = new();
    }

    public interface IUserGeneratedState
    {
        ExerciseStats exercise_stats { get; set; }
        object? user_data { get; set; }
        //object syncInfo { get; set; }
    }

    public class UserGeneratedState : IUserGeneratedState
    {
        public ExerciseStats exercise_stats { get; set; } = new();

        /// <summary>
        /// Seems to be "metaphor"-related data, e.g. { "equipped": { "suit01": {},.. }, "bodyParts": { "eyes01": {},... }, "inventory": { "suit02": { },...
        /// Why is this not in ExerciseStats.metaphorData?
        /// </summary>
        public object? user_data { get; set; }

        public object? syncInfo { get; set; } // TODO: what is this?
    }

    public class UserFullState : IUserServerSettings, IUserGeneratedState
    {
        public string uuid { get; set; } = "";
        public object training_plan { get; set; } = new(); //TrainingPlan
        public TrainingSettings training_settings { get; set; } = new();

        public ExerciseStats exercise_stats { get; set; } = new();
        public object? user_data { get; set; }

        public object? syncInfo { get; set; }
    }

    public class TrainingSettings
    {
        public List<decimal> timeLimits { get; set; } = new(); //time_limits
        public object? uniqueGroupWeights { get; set; }
        public List<string>? manuallyUnlockedExercises { get; set; }
        public decimal? idleTimeout { get; set; }
        public string cultureCode { get; set; } = "sv-SE";
        public CustomData? customData { get; set; }
        //training_settings: any;
        public List<TriggerData>? triggers { get; set; } 
        public decimal? pacifistRatio { get; set; } = 0.1M; //TODO: add to a metaphorSettings structure instead

        public object? trainingPlanOverrides { get; set; } //testData [{"id":"WM_grid#\\d+","phases":[{"lvlMgr":{"phaseChange":{"change":-0.8}}}]}]
        public TrainingSyncSettings? syncSettings { get; set; }
        //erase_local_data?: boolean;
        public bool? alarmClockInvisible { get; set; }

        /// <summary>
        /// Regex patterns of ITrainingAnalyzer type names to execute
        /// </summary>
        public List<string>? Analyzers { get; set; } 

        public static TrainingSettings Default => new TrainingSettings { timeLimits = new List<decimal> { 33 } };


        public void UpdateTrainingOverrides(IEnumerable<object> triggers, bool removePreExistingOverrides = true)
        {
            dynamic overrides;
            if (trainingPlanOverrides == null || removePreExistingOverrides)
            {
                overrides = new
                {
                    triggers = triggers.ToArray()
                };
            }
            else
            {
                overrides = trainingPlanOverrides;
                overrides.triggers = triggers.ToArray();
            }

            trainingPlanOverrides = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(overrides));
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

        public static dynamic CreateWeightChangeTrigger(Dictionary<string, int> weights, int trainingDay, int defaultWeight = 100)
        {
            //var knownGroups = new[] { "Math", "WM", "Reasoning" };
            //var knownExercises = new[] { "tangram#intro", "tangram", "rotation#intro", "rotation", "nvr_so", "nvr_rp" };
            //var weigthsDefault = knownGroups.Concat(knownExercises).ToDictionary(o => o, o => defaultWeight);

            var groupedExercises = new[] {
                new { Group = "Math", Exercises = new[] { "addsub", "npals", "numberline" } },
                new { Group = "WM", Exercises = new[] { "WM_grid#intro", "WM_grid", "WM_crush", "WM_3dgrid", "WM_circle", "WM_numbers#intro", "WM_numbers", "WM_moving" } },
                new { Group = "Reasoning", Exercises = new[] { "tangram#intro", "tangram", "rotation#intro", "rotation", "nvr_so", "nvr_rp", "boolean" } },
            };
            var weigthsDefault = groupedExercises.SelectMany(o => o.Exercises.Concat(new[] { o.Group })).ToDictionary(o => o, o => defaultWeight);

            foreach (var key in weigthsDefault.Keys.Except(weights.Keys))
                weights[key] = weigthsDefault[key];

            var trigger = CreateTrigger(trainingDay);
            trigger.actionData.properties.weights = ConvertToDynamicOrThrow(weights);

            return trigger;
        }

        public static dynamic ConvertToDynamicOrThrow(object obj)
        {
            var result = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(obj));
            if (result == null)
                throw new Exception("Couldn't deserialize");
            return result;
        }
    }

    public class TrainingSyncSettings
    {
        public bool eraseLocalData { get; set; } = false; //remove all local data
        public bool eraseLocalUserFullState { get; set; } = false; //remove server settings and user-generated state
        public bool eraseLocalLog { get; set; } = false; //clear LogItems
        public bool syncOnInit { get; set; } = true;
        public string defaultSyncUrl { get; set; } = string.Empty;
        public string routerUrl { get; set; } = string.Empty;
        public string syncTriggerCode { get; set; } = string.Empty; //TODO... Can't use runtime execution of string, will fail after minification... "{ performSync: logItem.isOfType(LeaveTestLogItem), pushState: logItem.isOfType(LeaveTestLogItem) }"
    }

    public class CustomData
    {
        public bool? menuButton { get; set; }
        public bool? canLogout { get; set; }
        public bool? unlockAllPlanets { get; set; }
        public object? appVersion { get; set; }
        public bool? allowMultipleLogins { get; set; }
        public bool? canEnterCompleted { get; set; }
        public object? nuArch { get; set; }
        public object? medalMode { get; set; } // "THREE_WINS" | "ONE_WIN" | "TARGET_SCORE" | "ALWAYS";
        public object? clearClientUserData { get; set; }
        public object? debugSync { get; set; }
        public object? numberLine { get; set; }
        public bool? displayAppVersion { get; set; }
    }

    public class TraininPlanData
    {
        public string metaphor { get; set; } = "";
        public bool? isTraining { get; set; }
        //public autoConnectType: string = "THREE-WAY";
        //public tests: GameDefinition[];
        public List<TriggerData>? triggers { get; set; }
        public bool? allowFreeChoice { get; set; }
        public int? targetTrainingDays { get; set; }
    }

    public class TriggerData
    {
        public string? type { get; set; }
        public TriggerTimeType triggerTime { get; set; } //string
        public object[] criteriaValues { get; set; } = new object[0];
        public TriggerActionData actionData { get; set; } = new TriggerActionData();
    }

    public enum TriggerTimeType
    {
        POST_RACE,
        POST_RACE_SUCCESS,
        POST_RACE_FAIL,
        LEAVE_TEST,
        END_OF_DAY,
        START_OF_DAY,
        MAP,
        MAP_POST_WIN
    }

    public class TriggerActionData
    {
        public string? type { get; set; }// TODO: currently instantiates by string, possibly other solution (register classes)
        public string id { get; set; } = "";
        public object? properties { get; set; }
    }

    public class ExerciseStats
    {
        public string appVersion { get; set; } = "";
        public string appBuildDate { get; set; } = "";
        public DeviceInfo device { get; set; } = new DeviceInfo();
        public int trainingDay { get; set; } = 0;

        //    lastLogin = (DateTime.Now - new DateTime(1970,1,1)).TotalMilliseconds,
        //    lastTimeStamp = (DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds,
        public long lastLogin { get; set; } = 0;
        public long lastTimeStamp { get; set; } = 0;
        public Dictionary<string, bool> triggerData { get; set; } = new();
        public List<GameRunStats> gameRuns { get; set; } = new List<GameRunStats>();
        public object? metaphorData { get; set; }
        public TrainingPlanSettings trainingPlanSettings { get; set; } = new TrainingPlanSettings();
        public Dictionary<string, object> gameCustomData { get; set; } = new Dictionary<string, object>();
    }

    public class TrainingPlanSettings
    {
        public Dictionary<string, decimal> initialGroupWeights { get; set; } = new Dictionary<string, decimal>();
        public List<TrainingPlanChange> changes { get; set; } = new List<TrainingPlanChange>();
    }

    public class TrainingPlanChange
    {
        public long timestamp { get; set; }
        public string type { get; set; } = "";
        public object? change { get; set; }
    }

    public class DeviceInfo
    {
        public string platform { get; set; } = "";
        public string model { get; set; } = "";
        public string version { get; set; } = "";
        public string uuid { get; set; } = "";
    }

    public class GameRunStats
    {
        public string gameId { get; set; } = "";
        //TODO: reintroduce this one later..? public isCompleted: boolean = false;
        //public medalCount: number = 0;
        //public bestScore: number = -1;
        //public bestTime: number = 60;
        public decimal lastLevel { get; set; } = 0;
        public decimal highestLevel { get; set; } = 0;
        //public noOfWins: number = 0;
        //public wonLast: boolean = false;
        public bool won { get; set; } = false;
        //public scores: Array<number> = [];
        //public stars: number = 0;
        //public lastTimeStamp: number = 0;
        public JsonDocument? customData { get; set; } = null;

        public long trainingTime { get; set; } = 0;
        public int trainingDay { get; set; } = 0;
        //public runsLeft: number = 3;

        public long started_at { get; set; } = 0;

        public bool cancelled { get; set; } = false;
    }
}
