namespace ProblemSource.Models.LogItems
{
    public class NewProblemLogItem : LogItem
    {
        public static string _type = "NEW_PROBLEM";
        public string problem_type { get; set; } = "";
        public string problem_string { get; set; } = "";
        public decimal level { get; set; } = 0; //Number.NaN;
    }
}
