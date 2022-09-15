namespace ProblemSource.Models
{
    public class SyncInput
    {
        public string ApiKey { get; set; }
        public string Uuid { get; set; }
        public string? SessionToken { get; set; }
        public bool RequestState { get; set; }
        public object[] Events { get; set; } // TODO: LogItem
        public bool ContinueOnEventsError { get; set; }
        public long CurrentTime { get; set; }
        public Device Device { get; set; }
        public string ClientApp { get; set; }
        public string? ClientVersion { get; set; }
        public int[] FPS { get; set; }
    }

    public class Device
    {
        public string platform { get; set; }
        public string model { get; set; }
        public string version { get; set; }
        public string uuid { get; set; }
    }

    public class LogItem
    {
        public string className { get; set; } = "";
        public long time { get; set; }
        public string type { get; set; } = "";
    }

    public class NewPhaseLogItem : LogItem
    {
        public static string _type = "NEW_PHASE";
        public int training_day { get; set; }
        public string exercise { get; set; } = "";
        public int sequence { get; set; }
        public string phase_type { get; set; } = ""; 
    }
    public class NewProblemLogItem : LogItem
    {
        public static string _type = "NEW_PROBLEM";
        public string problem_type { get; set; } = "";
        public string problem_string { get; set; } = "";
        public decimal level { get; set; } = 0; //Number.NaN;
    }
    public class PhaseEndLogItem : LogItem
    {
        public static string _type = "PHASE_END";
        public string phase { get; set; } = "";
        public int noOfQuestions { get; set; }
        public int noOfCorrect { get; set; } //corrects
        public int noOfIncorrect { get; set; } //incorrects

        public decimal score { get; set; }
        public decimal targetScore { get; set; }
        public decimal planetTargetScore { get; set; }
        public bool wonRace { get; set; }
        public bool completedPlanet { get; set; }
    }
    public class AnswerLogItem : LogItem
    {
        public static string _type = "ANSWER";
        public string answer { get; set; } = "";
        public string? correctAnswer { get; set; } //TODO: only set (in MathProblem), never used
        public bool correct { get; set; }
        public string? errorType { get; set; }
        public int tries { get; set; }
        public string? group { get; set; }
        public int response_time { get; set; } = 0;
    }

    public class UserStatePushLogItem : LogItem, IUserGeneratedState
    {
        public ExerciseStats exercise_stats { get; set; } = new ExerciseStats();
        public object? user_data { get; set; }
    }

}
