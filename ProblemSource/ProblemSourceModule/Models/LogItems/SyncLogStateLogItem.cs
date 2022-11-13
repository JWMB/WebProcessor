namespace ProblemSource.Models.LogItems
{
    public class SyncLogStateLogItem : LogItem
    {
        public static string _type = "ALREADY_SYNCED"; // "NOT_SYNCED";

        public bool syncedUpToHere { get; set; }
    }
}
