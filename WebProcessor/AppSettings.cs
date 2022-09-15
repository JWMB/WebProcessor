using ProblemSource.Services;

namespace WebApi
{
    public class AppSettings
    {
        public AzureTableConfig AzureTable { get; set; } = new();
        public string? SyncUrls { get; set; } = "";
    }
}
