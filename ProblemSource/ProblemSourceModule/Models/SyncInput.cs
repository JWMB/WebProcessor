namespace ProblemSource.Models
{
    public class SyncInput
    {
        //public string ApiKey { get; set; }
        public string Uuid { get; set; }
        public string? SessionToken { get; set; }
        public bool RequestState { get; set; }
        public object[] Events { get; set; } // TODO: LogItem[]
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
}
