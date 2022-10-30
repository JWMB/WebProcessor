namespace ProblemSource.Models
{
    public class SyncResult
    {
        public string? error { get; set; }
        public string? warning { get; set; }
        public string sessionToken { get; set; } = Guid.NewGuid().ToString(); //TODO: How does this work?
        public string state { get; set; } = ""; // client expects a stringified UserFullState, not JSON

        public string messages { get; set; } = "";
        public bool phasesInsertFail { get; set; }
    }
}
