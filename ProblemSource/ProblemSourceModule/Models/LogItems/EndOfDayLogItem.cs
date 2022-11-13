namespace ProblemSource.Models.LogItems
{
    public class EndOfDayLogItem : LogItem
    {
        public static string _type = "END_OF_DAY";

        public int training_day { get; set; }
    }
}
