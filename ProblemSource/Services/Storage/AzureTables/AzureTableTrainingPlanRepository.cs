using Azure.Data.Tables;
using ProblemSource.Models;

namespace ProblemSource.Services.Storage.AzureTables
{
    public class AzureTableTrainingPlanRepository : ITrainingPlanRepository
    {
        private readonly TableClient client;

        public AzureTableTrainingPlanRepository(AzureTableConfig config)
        {
            client = config.CreateTableClient(config.TableTrainingPlans);
        }

        public async Task<TrainingPlan?> Get(string name)
        {
            await client.CreateIfNotExistsAsync();
            var partitionKey = name;
            var queryResultsFilter = client.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{partitionKey}'"); //and RowKey eq '{uuid}'

            await foreach (var entity in queryResultsFilter)
            {
                var str = AzureTableConfig.GetLongString(entity);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<LinearTrainingPlan>(str);
            }

            return null;
        }
    }
}
