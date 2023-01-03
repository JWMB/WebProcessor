using ProblemSource.Services.Storage.AzureTables;

namespace TrainingApi
{
    public class AppSettings
    {
        public AzureTableConfig AzureTable { get; set; } = new();
    }
}
