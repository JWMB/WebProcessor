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

        private readonly string staticPartitionKey = "none";

        public AzureTableTrainingRepository(ITypedTableClientFactory tableClientFactory)
        {
            tableClient = tableClientFactory.Trainings;
            converter = new ExpandableTableEntityConverter<Training>(t => (staticPartitionKey, AzureTableConfig.IdToKey(t.Id)));
            repo = new TableEntityRepository<Training, TableEntity>(tableClient, converter.ToPoco, converter.FromPoco, staticPartitionKey);
        }

        public async Task<Training?> Get(int id) => await repo.Get(AzureTableConfig.IdToKey(id));

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
        public async Task<int> Upsert(Training item) => int.Parse(await repo.Upsert(item));

        public async Task Remove(Training item) => await repo.Remove(item);

        public async Task<IEnumerable<Training>> GetAll() => await repo.GetAll();

        public async Task<IEnumerable<Training>> GetByIds(IEnumerable<int> ids)
        {
            var values = await repo.GetByRowKeys(ids.Select(AzureTableConfig.IdToKey)); // staticPartitionKey
            // Note: skips entries that were not found
            return values.Values.OfType<Training>();
        }
    }
}
