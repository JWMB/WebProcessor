using Azure.Data.Tables;

namespace ProblemSource.Services.Storage
{
    public interface ITableClientFactory
    {
        TableClient Phases { get; }
        TableClient TrainingDays { get; }
    }

    public class TableClientFactory : ITableClientFactory
    {
        private readonly string prefix = "vektor";
        private readonly string connectionString = "UseDevelopmentStorage=true";
        private readonly TableClientOptions tableClientOptions;
        public TableClientFactory()
        {
            tableClientOptions = new TableClientOptions();
            tableClientOptions.Retry.MaxRetries = 1;
        }

        public async Task Init()
        {
            await Phases.CreateIfNotExistsAsync();
            await TrainingDays.CreateIfNotExistsAsync();
        }

        public TableClient Phases => CreateClient(nameof(Phases));
        public TableClient TrainingDays => CreateClient(nameof(TrainingDays));

        private TableClient CreateClient(string name) => new TableClient(connectionString, $"{prefix}{name}", tableClientOptions);
    }
}
