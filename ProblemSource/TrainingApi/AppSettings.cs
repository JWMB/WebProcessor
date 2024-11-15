using ProblemSource.Services.Storage.AzureTables;
using TrainingApi.RealTime;

namespace TrainingApi
{
    public class AppSettings
    {
        public AzureTableConfig AzureTable { get; set; } = new();
        public RealTimeConfig RealTime { get; set; } = new();
        public string? SyncUrls { get; set; } = "";
    }
}
