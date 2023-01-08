using Azure.Data.Tables;
using Common.Web.Services;

namespace ProblemSource.Services.Storage.AzureTables
{
    public interface ITypedTableClientFactory : ITableClientFactory
    {
        TableClient Phases { get; }
        TableClient TrainingDays { get; }
        TableClient PhaseStatistics { get; }
        TableClient TrainingSummaries { get; }
        TableClient Trainings { get; }
        TableClient UserStates { get; }
        TableClient Users { get; }
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

        public IEnumerable<TableClient> AllClients()
        {
            var props = GetType().GetProperties()
                .Where(o => o.CanRead && !o.CanWrite)
                .Where(o => o.PropertyType == typeof(TableClient));

            foreach (var prop in props)
            {
                var client = prop.GetValue(this) as TableClient;
                if (client == null)
                {
                    throw new NullReferenceException($"TableClient for '{prop.Name}' is null");
                }
                yield return client;
            }
        }

        public async Task Init()
        {
            foreach (var client in AllClients())
            {
                try
                {
                    await client.CreateIfNotExistsAsync();
                }
                catch (Exception ex)
                {
                    if (ex.ToString().Contains("127.0.0.1:"))
                        throw new Exception("Could not connect to Storage Emulator - have you started it? See Azurite, https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio");
                    throw;
                }
            }
        }

        public TableClient Phases => CreateClient(nameof(Phases));
        public TableClient TrainingDays => CreateClient(nameof(TrainingDays));
        public TableClient PhaseStatistics => CreateClient(nameof(PhaseStatistics));
        public TableClient TrainingSummaries => CreateClient(nameof(TrainingSummaries));
        public TableClient Trainings => CreateClient(nameof(Trainings));
        public TableClient UserStates => CreateClient(nameof(UserStates));
        public TableClient Users => CreateClient(nameof(Users));

        public TableClient CreateClient(string name) => new TableClient(connectionString, $"{prefix}{name}", tableClientOptions);

        public async Task<TableClient> CreateClientAndInit(string name)
        {
            var client = CreateClient(name);
            await client.CreateIfNotExistsAsync();
            return client;
        }
    }
}
