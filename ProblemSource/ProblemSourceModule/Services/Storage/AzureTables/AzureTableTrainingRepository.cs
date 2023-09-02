using Azure.Data.Tables;
using AzureTableGenerics;
using ProblemSource.Services.Storage.AzureTables;
using ProblemSource.Services.Storage.AzureTables.TableEntities;
using ProblemSourceModule.Models;

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
            converter = new ExpandableTableEntityConverter<Training>(t => new TableFilter(staticPartitionKey, AzureTableConfig.IdToKey(t.Id)));
            repo = new TableEntityRepository<Training, TableEntity>(tableClient, converter.ToPoco, converter.FromPoco, new TableFilter(staticPartitionKey));
        }

        public async Task<Training?> Get(int id) => await repo.Get(AzureTableConfig.IdToKey(id));

        private static int latestMax = 0; // TODO: ugly performance hack while waiting to port to SQL
        private object _lock = new object();
        public Task<int> Add(Training item)
        {
            // Warning: multi-instance concurrency 
            lock (_lock)
            {
                string? q = latestMax > 0 ? $"RowKey ge '{AzureTableConfig.IdToKey(latestMax)}'" : null;
                var max = tableClient.Query<TableEntity>(q).OrderByDescending(o => o.RowKey).FirstOrDefault();
                item.Id = int.Parse(max?.RowKey ?? "0") + 1;
                latestMax = item.Id;
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
