namespace ProblemSource.Models
{
    public class SyncInput
    {
        //public string ApiKey { get; set; }
        public string Uuid { get; set; } = string.Empty;
        public string? SessionToken { get; set; }
        public bool RequestState { get; set; }
        public object[] Events { get; set; } = new object[0]; // TODO: LogItem[] ?
        public bool ContinueOnEventsError { get; set; }
        public long CurrentTime { get; set; }
        public Device Device { get; set; } = new Device();
        public string ClientApp { get; set; } = string.Empty;
        public string? ClientVersion { get; set; }
        public int[] FPS { get; set; } = new int[0];
    }

    public class Device
    {
        public string platform { get; set; } = string.Empty;
        public string model { get; set; } = string.Empty;
        public string version { get; set; } = string.Empty;
        public string uuid { get; set; } = string.Empty;
    }
}
