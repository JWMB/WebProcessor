namespace ProblemSource.Models.LogItems
{
    public class LeaveTestLogItem : LogItem
    {
        public static string _type = "LEAVE_TEST";

        public int training_day { get; set; }
    }
}
