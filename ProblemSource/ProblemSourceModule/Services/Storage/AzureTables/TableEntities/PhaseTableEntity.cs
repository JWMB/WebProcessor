using Azure.Data.Tables;
using Azure;
using Newtonsoft.Json;
using ProblemSource.Models.Aggregates;

namespace ProblemSource.Services.Storage.AzureTables.TableEntities
{
    public class PhaseTableEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public int id { get; set; }
        public int training_day { get; set; }
        public string exercise { get; set; } = string.Empty;
        public string phase_type { get; set; } = string.Empty;
        public string time { get; set; } = "0"; // long is not a supported data type, convert to string
        public int sequence { get; set; }

        public string problemsSerialized { get; set; } = string.Empty;
        public string userTestSerialized { get; set; } = string.Empty;

        public Phase ToBusinessObject()
        {
            var problems = JsonConvert.DeserializeObject<List<Problem>>(problemsSerialized);
            return new Phase
            {
                exercise = exercise,
                phase_type = phase_type,
                sequence = sequence,
                time = long.Parse(time),
                training_day = training_day,
                problems = problems ?? new List<Problem>(),
                user_test = JsonConvert.DeserializeObject<UserTest>(userTestSerialized)
            };
        }

        public static PhaseTableEntity FromBusinessObject(Phase p, string userId) => new PhaseTableEntity
        {
            exercise = p.exercise,
            phase_type = p.phase_type,
            sequence = p.sequence,
            time = p.time.ToString(),
            training_day = p.training_day,
            problemsSerialized = JsonConvert.SerializeObject(p.problems),
            userTestSerialized = JsonConvert.SerializeObject(p.user_test),

            PartitionKey = userId,
            RowKey = Phase.UniqueIdWithinUser(p),
        };
    }
}
