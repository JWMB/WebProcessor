using Azure.Data.Tables;
using ProblemSource.Services.Storage.AzureTables;
using ProblemSource.Services.Storage.AzureTables.TableEntities;

namespace ProblemSourceModule.Services.Storage.AzureTables
{
    public class AzureTableTrainingRepository : ITrainingRepository
    {
        private readonly TableEntityRepository<Training, TableEntity> repo;
        private readonly ExpandableTableEntityConverter<Training> converter;
        private readonly TableClient tableClient;

        public AzureTableTrainingRepository(ITypedTableClientFactory tableClientFactory)
        {
            var staticPartitionKey = "none";

            tableClient = tableClientFactory.Trainings;
            converter = new ExpandableTableEntityConverter<Training>(t => (staticPartitionKey, t.Id.ToString()));
            repo = new TableEntityRepository<Training, TableEntity>(tableClient, converter.ToPoco, converter.FromPoco, staticPartitionKey);
        }

        public async Task<Training?> Get(int id) => await repo.Get(id.ToString());

        private object _lock = new object();
        public Task<int> Add(Training item)
        {
            // Warning: multi-instance concurrency 
            lock (_lock)
            {
                var max = tableClient.Query<TableEntity>().OrderByDescending(o => o.RowKey).FirstOrDefault(); //.OrderBy(o => o.).LastOrDefault;
                item.Id = int.Parse(max?.RowKey ?? "0") + 1;
                return Task.FromResult(int.Parse(repo.Add(item).Result));
            }
        }

        public async Task Update(Training item) => await repo.Update(item);
        public async Task Remove(Training item) => await repo.Remove(item);
    }
}
