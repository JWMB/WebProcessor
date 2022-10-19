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
            return JsonConvert.DeserializeObject<TrainingDayAccount>(Data);
        }

        public static TrainingDayTableEntity FromBusinessObject(TrainingDayAccount p) => new TrainingDayTableEntity
        {
            Data = JsonConvert.SerializeObject(p),

            PartitionKey = p.AccountUuid,
            RowKey = $"{p.TrainingDay}"
        };
    }
}
