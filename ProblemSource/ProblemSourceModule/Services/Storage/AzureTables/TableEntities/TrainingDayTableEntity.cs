using Azure.Data.Tables;
using Azure;
using Newtonsoft.Json;
using ProblemSource.Models.Aggregates;

namespace ProblemSource.Services.Storage.AzureTables.TableEntities
{
    public class TrainingDayTableEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string Data { get; set; } = string.Empty;

        public TrainingDayAccount ToBusinessObject()
        {
            var result = JsonConvert.DeserializeObject<TrainingDayAccount>(Data);
            if (result == null) throw new Exception($"Couldn't deserialize {nameof(TrainingDayAccount)}: {PartitionKey}/{RowKey}");
            if (result.AccountId == 0)
                result.AccountId = AzureTableConfig.KeyToId(PartitionKey);

            return result;
        }

        public static TrainingDayTableEntity FromBusinessObject(TrainingDayAccount p) => new TrainingDayTableEntity
        {
            Data = JsonConvert.SerializeObject(p),

            PartitionKey = AzureTableConfig.IdToKey(p.AccountId),
            RowKey = $"{p.TrainingDay}"
        };
    }
}
