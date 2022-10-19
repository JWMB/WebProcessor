using Azure.Data.Tables;
using System.Collections.Concurrent;
using ProblemSource.Models;
using Azure;

namespace ProblemSource.Services
{
    public interface IUserStateRepository
    {
        Task Set(string uuid, object state); //IUserGeneratedState
        Task<object?> Get(string uuid);
        Task<T?> Get<T>(string uuid) where T : class;
    }

    public class InMemoryUserStateRepository : IUserStateRepository
    {
        private static ConcurrentDictionary<string, object> userStates = new ConcurrentDictionary<string, object>(); //IUserGeneratedState
        public Task Set(string uuid, object state)
        {
            userStates.AddOrUpdate(uuid, state, (s1, s2) => state);
            return Task.CompletedTask;
            //await File.WriteAllTextAsync(@"", Newtonsoft.Json.JsonConvert.SerializeObject(state));
        }

        public Task<object?> Get(string uuid) => Task.FromResult(userStates.GetValueOrDefault(uuid));

        public Task<T?> Get<T>(string uuid) where T : class => Task.FromResult(userStates.GetValueOrDefault(uuid) as T);
    }

    public class AzureTableConfig
    {
        //public string StorageUri { get; set; }
        //public string? AccountName { get; set; }
        //public string? StorageAccountKey { get; set; }

        public string ConnectionString { get; set; } = "";
        public string TableUserStates { get; set; } = "";
        public string TableUserLogs { get; set; } = "";
        public string TableTrainingPlans { get; set; } = "";

        //public TableSharedKeyCredential CreateCredentials() =>
        //    new TableSharedKeyCredential(AccountName, StorageAccountKey);
        //public TableServiceClient CreateServiceClient() =>
        //    new TableServiceClient(new Uri(StorageUri), CreateCredentials());
        //public TableClient CreateTableClient(string tableName) =>
        //    new TableClient(new Uri(StorageUri), tableName, CreateCredentials());

        public TableServiceClient CreateServiceClient() => new TableServiceClient(ConnectionString); // new Uri(StorageUri));

        public TableClient CreateTableClient(string tableName) => new TableClient(ConnectionString, tableName); // new Uri(StorageUri), tableName, CreateCredentials());

        public static void SetLongString(TableEntity entity, string str, string prefix = "Data")
        {
            var max = 32 * 1024;
            for (int i = 0; i < (int)Math.Ceiling((decimal)str.Length / max); i++)
            {
                var index = i * max;
                entity.Add($"{prefix}{i}", str.Substring(index, Math.Min(max, str.Length - index)));
            }
        }
        public static string GetLongString(TableEntity entity, string prefix = "Data")
        {
            var sb = new System.Text.StringBuilder();
            int i = 0;
            while (true)
            {
                if (entity.TryGetValue($"{prefix}{i++}", out var obj))
                    sb.Append(obj.ToString());
                else
                    break;
            }
            return sb.ToString();
        }
    }

    public class AzureTableUserStateRepository : IUserStateRepository
    {
        private readonly AzureTableConfig config;
        private readonly string partitionKey = "p";

        public AzureTableUserStateRepository(AzureTableConfig config)
        {
            this.config = config;
        }

        public async Task<object?> Get(string uuid)
        {
            var tableClient = config.CreateTableClient(config.TableUserStates);
            await tableClient.CreateIfNotExistsAsync();
            var queryResultsFilter = tableClient.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{partitionKey}' and RowKey eq '{uuid}'");

            await foreach (var entity in queryResultsFilter)
            {
                var str = AzureTableConfig.GetLongString(entity);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(str);
            }

            return null;
        }

        public async Task<T?> Get<T>(string uuid) where T : class
        {
            var obj = await Get(uuid);
            return obj == null ? null : Newtonsoft.Json.JsonConvert.DeserializeObject<T>(Newtonsoft.Json.JsonConvert.SerializeObject(obj));
        }

        public async Task Set(string uuid, object state)
        {
            var entity = new TableEntity(partitionKey, uuid);
            AzureTableConfig.SetLongString(entity, Newtonsoft.Json.JsonConvert.SerializeObject(state));

            await config.CreateTableClient(config.TableUserStates).UpsertEntityAsync(entity, TableUpdateMode.Replace);
        }
    }

    //class TableEntityBase : ITableEntity
    //{
    //    public string PartitionKey { get; set; }
    //    public string RowKey { get; set; }
    //    public DateTimeOffset? Timestamp { get; set; }
    //    public ETag ETag { get; set; }

    //    public TableEntityBase()
    //    {
    //    }
    //    public TableEntityBase(string partitionKey, string rowKey)
    //    {
    //        PartitionKey = partitionKey;
    //        RowKey = rowKey;
    //    }
    //}

    //internal class AzStorageEntityAdapter<T> : ITableEntity where T : TableEntityBase, new()
    //{
    //    /// <summary>
    //    /// Gets or sets the entity's partition key
    //    /// </summary>
    //    public string PartitionKey
    //    {
    //        get => InnerObject.PartitionKey;
    //        set => InnerObject.PartitionKey = value;
    //    }

    //    /// <summary>
    //    /// Gets or sets the entity's row key.
    //    /// </summary>
    //    public string RowKey
    //    {
    //        get => InnerObject.RowKey;
    //        set => InnerObject.RowKey = value;
    //    }

    //    /// <summary>
    //    /// Gets or sets the entity's Timestamp.
    //    /// </summary>
    //    public DateTimeOffset? Timestamp
    //    {
    //        get => InnerObject.Timestamp;
    //        set => InnerObject.Timestamp = value;
    //    }

    //    /// <summary>
    //    /// Gets or sets the entity's current ETag.
    //    /// Set this value to '*' in order to blindly overwrite an entity as part of an update operation.
    //    /// </summary>
    //    public ETag ETag
    //    {
    //        get => InnerObject.ETag;
    //        set => InnerObject.ETag = value;
    //    }

    //    /// <summary>
    //    /// Place holder for the original entity
    //    /// </summary>
    //    public T InnerObject { get; set; }

    //    public AzStorageEntityAdapter()
    //    {
    //        // If you would like to work with objects that do not have a default Ctor you can use (T)Activator.CreateInstance(typeof(T));
    //        InnerObject = new T();
    //    }

    //    public AzStorageEntityAdapter(T innerObject)
    //    {
    //        InnerObject = innerObject;
    //    }

    //    public virtual void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
    //    {
    //        TableEntity.ReadUserObject(this.InnerObject, properties, operationContext);
    //    }

    //    public virtual IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
    //    {
    //        return TableEntity.WriteUserObject(this.InnerObject, operationContext);
    //    }
    //}
}
