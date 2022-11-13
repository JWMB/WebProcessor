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
}
