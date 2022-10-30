using Azure.Data.Tables;

namespace ProblemSource.Services.Storage.AzureTables
{
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
