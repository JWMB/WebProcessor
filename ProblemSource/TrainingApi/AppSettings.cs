using ProblemSource.Services.Storage.AzureTables;

namespace TrainingApi
{
    public class AppSettings
    {
        public AzureTableConfig AzureTable { get; set; } = new();
        public AzureQueueConfig AzureQueue { get; set; } = new();
        public string? SyncUrls { get; set; } = "";
    }

    public class AzureQueueConfig
    {
        public string ConnectionString { get; set; } = "";
        public string RealtimeEventQueueName { get; set; } = "";
    }
}
