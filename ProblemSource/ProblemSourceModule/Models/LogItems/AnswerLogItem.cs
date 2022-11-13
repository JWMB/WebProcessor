namespace ProblemSource.Models.LogItems
{
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
}
