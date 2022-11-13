using Azure.Data.Tables;
using System.Runtime.CompilerServices;

namespace ProblemSource.Services.Storage.AzureTables
{
    public interface ITableClientFactory
    {
        TableClient Phases { get; }
        TableClient TrainingDays { get; }
        TableClient PhaseStatistics { get; }
        TableClient Trainings { get; }
    }

    public class TableClientFactory : ITableClientFactory
    {
        private readonly string prefix = "vektor";
        private readonly string connectionString;
        private readonly TableClientOptions tableClientOptions;
        public TableClientFactory(string? connectionString)
        {
            this.connectionString = string.IsNullOrEmpty(connectionString) ? "UseDevelopmentStorage=true" : connectionString;
            tableClientOptions = new TableClientOptions();
            tableClientOptions.Retry.MaxRetries = 1;
        }

        public async Task Init()
        {
            await Phases.CreateIfNotExistsAsync();
            await TrainingDays.CreateIfNotExistsAsync();
            await PhaseStatistics.CreateIfNotExistsAsync();
            await Trainings.CreateIfNotExistsAsync();
        }

        public TableClient Phases => CreateClient(nameof(Phases));
        public TableClient TrainingDays => CreateClient(nameof(TrainingDays));
        public TableClient PhaseStatistics => CreateClient(nameof(PhaseStatistics));
        public TableClient Trainings => CreateClient(nameof(Trainings));

        public TableClient CreateClient(string name) => new TableClient(connectionString, $"{prefix}{name}", tableClientOptions);

        public async Task<TableClient> CreateClientAndInit(string name)
        {
            var client = CreateClient(name);
            await client.CreateIfNotExistsAsync();
            return client;
        }

        public static async Task<TableClientFactory> Create(string? connectionString)
        {
            var tableFactory = new TableClientFactory(connectionString);
            try
            {
                await tableFactory.Init();
            }
            catch (Exception ex)
            {
                if (ex.ToString().Contains("127.0.0.1:"))
                    throw new Exception("Could not connect to Storage Emulator - have you started it? See Azurite, https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio");
                throw;
            }
            return tableFactory;
        }
    }
}
