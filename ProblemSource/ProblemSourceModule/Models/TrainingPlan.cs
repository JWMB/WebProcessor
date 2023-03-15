namespace ProblemSource.Models
{
    public class GameDefinition
    {
        public bool invisible { get; set; }

        public string id { get; set; } = "";
        public string title { get; set; } = "";
        public List<object> phases { get; set; } = new List<object>();

        public string progVisualizer { get; set; } = "";
        public object? progVisualizerData { get; set; }
    }

    public class LinearGameDefinition : GameDefinition
    {
        public string isCloneOf { get; set; } = "";
        public int cloneNo { get; set; } = 1;
        public bool notVisibleOnMenu { get; set; } = false; //TODO: replace with GameDefinition::invisible
        public List<string> requires { get; set; } = new List<string>();
    }

    public class TrainingPlan
    {
        //public isPreProcessed:boolean=false;
        public string metaphor { get; set; } = "";
        public bool? isTraining { get; set; }
        //public autoConnectType: string = "THREE-WAY";
        //public tests: GameDefinition[];
        public List<TriggerData>? triggers { get; set; }
        public bool? allowFreeChoice { get; set; }
        public int? targetTrainingDays { get; set; }

        public ClientRequirements? clientRequirements { get; set; }
        public class ClientRequirements
        {
            public VersionRange? Version { get; set; }

            public class VersionRange
            {
                public string? Min { get; set; }
                public string? Max { get; set; }
            }
        }
    }

    public class LinearTrainingPlan : TrainingPlan
    {
        public bool isPreProcessed { get; set; } = false;
        public string autoConnectType { get; set; } = "THREE-WAY"; // ConnectionType
        public List<LinearGameDefinition> tests { get; set; } = new List<LinearGameDefinition>(); //LinearExerciseDefinition[];
    }

    public class DynamicTrainingPlan : TrainingPlan
    {
        public enum ValueTypeEnum
        {
            Runs,
            Time,
            Wins,
            Level,
        }
        public ValueTypeEnum valueType { get; set; } = ValueTypeEnum.Runs;

        public int minUnlockedExercises { get; set; } = 1;
        public int maxUnlockedExercises { get; set; } = 1;

        public List<GameDefinition> hiddenExercises { get; set; } = new();
        public bool preventSameExerciseTwice { get; set; }
    }
}
