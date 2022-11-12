namespace ProblemSource.Models.LogItems
{
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

        public TimesRegisterStimuliResponse? timings { get; set; } //TODO: check if this actually gets sent

        public class TimesRegisterStimuliResponse
        {
            public int? StimuliStart { get; set; }
            public int? StimuliEnd { get; set; }
            public int? ResponseAllowedStart { get; set; }
            public List<int?>? ResponseTimes { get; set; }
            public int? FeedbackStart { get; set; }
            public int? FeedbackEnd { get; set; }
        }
    }

    public class UserStatePushLogItem : LogItem, IUserGeneratedState
    {
        public ExerciseStats exercise_stats { get; set; } = new ExerciseStats();
        public object? user_data { get; set; }
    }

    public class EndOfDayLogItem : LogItem
    {
        public static string _type = "END_OF_DAY";

        public int training_day { get; set; }
    }

    public class LeaveTestLogItem : LogItem
    {
        public static string _type = "LEAVE_TEST";

        public int training_day { get; set; }
    }

    public class SyncLogStateLogItem : LogItem
    {
        public static string _type = "ALREADY_SYNCED"; // "NOT_SYNCED";

        public bool syncedUpToHere { get; set; }
    }
    public class SyncInfoLogItem : LogItem
    {
        public static string _type = "SYNC";

        public bool syncedUpToHere { get; set; }

        public bool isStartMarker { get; set; }
        public bool success { get; set; }
        public string? error { get; set; }
    }


    public class ErrorLogItem : LogItem
    {
        public static string _type = "ERROR";

        //timeStamp: Date;
        public string level { get; set; } = string.Empty; //error level (INFO, WARNING, ERROR, FATAL etc)
        public object[]? messages { get; set; }
        public object? exception { get; set; }
    }
}
