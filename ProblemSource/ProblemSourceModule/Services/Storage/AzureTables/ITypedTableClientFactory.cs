using Azure.Data.Tables;
using Common.Web.Services;

namespace ProblemSource.Services.Storage.AzureTables
{
    public interface ITypedTableClientFactory : ITableClientFactory
    {
        TableClient Phases { get; }
        TableClient TrainingDays { get; }
        TableClient PhaseStatistics { get; }
        TableClient Trainings { get; }
        TableClient UserStates { get; }
    }

    public class TypedTableClientFactory : ITypedTableClientFactory
    {
        private readonly string prefix = "vektor";
        private readonly string connectionString;
        private readonly TableClientOptions tableClientOptions;
        public TypedTableClientFactory(AzureTableConfig config)
        {
            prefix = config.TablePrefix;
            connectionString = string.IsNullOrEmpty(config.ConnectionString) ? "UseDevelopmentStorage=true" : config.ConnectionString;
            tableClientOptions = new TableClientOptions();
            tableClientOptions.Retry.MaxRetries = 1;
        }

        public async Task Init()
        {
            await Phases.CreateIfNotExistsAsync();
            await TrainingDays.CreateIfNotExistsAsync();
            await PhaseStatistics.CreateIfNotExistsAsync();
            await Trainings.CreateIfNotExistsAsync();
            await UserStates.CreateIfNotExistsAsync();
        }

        public TableClient Phases => CreateClient(nameof(Phases));
        public TableClient TrainingDays => CreateClient(nameof(TrainingDays));
        public TableClient PhaseStatistics => CreateClient(nameof(PhaseStatistics));
        public TableClient Trainings => CreateClient(nameof(Trainings));
        public TableClient UserStates => CreateClient(nameof(UserStates));

        public TableClient CreateClient(string name) => new TableClient(connectionString, $"{prefix}{name}", tableClientOptions);

        public async Task<TableClient> CreateClientAndInit(string name)
        {
            var client = CreateClient(name);
            await client.CreateIfNotExistsAsync();
            return client;
        }

        public static async Task<TypedTableClientFactory> Create(AzureTableConfig config)
        {
            var tableFactory = new TypedTableClientFactory(config);
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
