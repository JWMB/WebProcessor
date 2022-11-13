namespace ProblemSource.Models.LogItems
{
    public class SyncInfoLogItem : LogItem
    {
        public static string _type = "SYNC";

        public bool syncedUpToHere { get; set; }

        public bool isStartMarker { get; set; }
        public bool success { get; set; }
        public string? error { get; set; }
    }
}
