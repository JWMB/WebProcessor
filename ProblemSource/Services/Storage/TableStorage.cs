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
        //private Dictionary<string, bool> verifiedTableClients = new();

        //public async Task<TableClient?> Create(string name, string? connectionName = null)
        //{
        //    var key = $"{name}_{connectionName}";
        //    var client = new TableClient(connectionName ?? "UseDevelopmentStorage=true", name);
        //    if (verifiedTableClients.TryGetValue(key, out var wasVerified))
        //    {
        //        return wasVerified ? client : null;
        //    }
        //    try
        //    {
        //        await client.CreateIfNotExistsAsync();
        //    }
        //    catch (Exception ex)
        //    {
        //        // TODO: Log
        //        verifiedTableClients.Add(key, false);
        //        return null;
        //    }
        //    verifiedTableClients.Add(key, true);
        //    return client;
        //}
    }

    public class TableEntityRepository<T, TTableEntity> : IRepository<T> where TTableEntity : class, ITableEntity, new()
    {
        private readonly TableClient tableClient;
        private readonly Func<TTableEntity, T> toBusinessObject;
        private readonly Func<T, TTableEntity> toTableEntity;
        private readonly string partitionKeyForFilter = "0"; // userId

        public TableEntityRepository(TableClient tableClient, Func<TTableEntity, T> toBusinessObject, Func<T, TTableEntity> toTableEntity, string partitionKeyForFilter)
        {
            this.tableClient = tableClient;
            this.toBusinessObject = toBusinessObject;
            this.toTableEntity = toTableEntity;
            this.partitionKeyForFilter = partitionKeyForFilter;
        }

        public async Task<(IEnumerable<T> Added, IEnumerable<T> Updated)> AddOrUpdate(IEnumerable<T> items)
        {
            if (!items.Any())
                return (new List<T>(), new List<T>());

            var tableEntities = items.Select(toTableEntity).ToList();

            var batch = new List<TableTransactionAction>(tableEntities.Select(f => new TableTransactionAction(TableTransactionActionType.UpsertMerge, f)));
            try
            {
                var response = await tableClient.SubmitTransactionAsync(batch);
                if (response.Value.Any(o => o.IsError))
                    throw new Exception("rrrr");

                var itemAndStatus = items.Select((o, i) => new { Item = o, Status = response.Value[i].Status });
                return (itemAndStatus.Where(o => o.Status == 201).Select(o => o.Item), itemAndStatus.Where(o => o.Status != 201).Select(o => o.Item));
            }
            catch (TableTransactionFailedException ttfEx)
            {
                throw ttfEx;
            }
        }

        public async Task<IEnumerable<T>> GetAll()
        {
            var filter = $"{nameof(ITableEntity.PartitionKey)} eq '{partitionKeyForFilter}'";
            var query = tableClient.QueryAsync<TTableEntity>(filter);
            var result = new List<T>();
            await foreach (var entity in query)
            {
                result.Add(toBusinessObject(entity));
                //var str = AzureTableConfig.GetLongString(entity);
            }
            return result;
        }
    }
}
