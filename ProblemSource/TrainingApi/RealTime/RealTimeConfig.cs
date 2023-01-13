namespace TrainingApi.RealTime
{
    public class RealTimeConfig
    {
        public bool Enabled { get; set; } = false;
        public AzureQueueConfig? AzureQueueConfig { get; set; }
    }
}
