using Azure.Data.Tables;
using Azure;

namespace ProblemSource.Services.Storage.AzureTables.TableEntities
{
    public abstract class TableEntityBase : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
