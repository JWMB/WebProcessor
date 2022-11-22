using Azure;
using Azure.Data.Tables;
using Newtonsoft.Json;
using ProblemSourceModule.Services.Storage;
using System.Text.RegularExpressions;

namespace ProblemSource.Services.Storage.AzureTables
{
    public class TableEntityRepository<T, TTableEntity> : IRepository<T, string>, IBatchRepository<T> where TTableEntity : class, ITableEntity, new()
    {
        private readonly TableClient tableClient;
        private readonly Func<TTableEntity, T> toBusinessObject;
        private readonly Func<T, TTableEntity> toTableEntity;
        private readonly string partitionKeyForFilter; // userId

        public TableEntityRepository(TableClient tableClient, Func<TTableEntity, T> toBusinessObject, Func<T, TTableEntity> toTableEntity, string partitionKeyForFilter)
        {
            this.tableClient = tableClient;
            this.toBusinessObject = toBusinessObject;
            this.toTableEntity = toTableEntity;
            this.partitionKeyForFilter = partitionKeyForFilter;
        }

        private async Task<List<Response>> Upsert(IEnumerable<ITableEntity> entities)
        {
            var result = new List<Response>();
            foreach (var item in entities)
            {
                var response = await tableClient.UpsertEntityAsync(item, TableUpdateMode.Replace);
                if (response.IsError)
                    throw new Exception($"{response.ReasonPhrase}");
                result.Add(response);
            }
            return result;
        }

        private async Task<List<Response>> UpsertBatch(IEnumerable<ITableEntity> entities)
        {
            var batch = new List<TableTransactionAction>(entities.Select(f => new TableTransactionAction(TableTransactionActionType.UpsertMerge, f)));

            var response = await tableClient.SubmitTransactionAsync(batch);
            if (response.Value.Any(o => o.IsError))
                throw new Exception($"SubmitTransaction errors: {string.Join("\n", response.Value.Where(o => o.IsError).Select(o => o.ReasonPhrase))}");

            return response.Value.ToList();
        }

        // https://learn.microsoft.com/en-us/rest/api/storageservices/Understanding-the-Table-Service-Data-Model#characters-disallowed-in-key-fields
        public static Regex InvalidKeyRegex = new Regex(@"\/|\\|#|\?");

        public async Task<(IEnumerable<T> Added, IEnumerable<T> Updated)> AddOrUpdate(IEnumerable<T> items)
        {
            if (!items.Any())
                return (new List<T>(), new List<T>());

            var tableEntities = items.Select(toTableEntity).ToList();

            var invalidEntries = tableEntities.Where(o => InvalidKeyRegex.IsMatch(o.RowKey) || InvalidKeyRegex.IsMatch(o.PartitionKey));
            if (invalidEntries.Any())
            {
                throw new Exception($"{typeof(T).Name}: Invalid key(s) for {string.Join(",", tableEntities.Select(o => $"{o.PartitionKey}/{o.RowKey}"))}");
            }

            try
            {
                var responses = await UpsertBatch(tableEntities);

                var itemAndStatus = items.Select((o, i) => new { Item = o, responses[i].Status });
                return (itemAndStatus.Where(o => o.Status == 201).Select(o => o.Item), itemAndStatus.Where(o => o.Status != 201).Select(o => o.Item));
            }
            catch (Exception ex) when (ex is TableTransactionFailedException || ex is RequestFailedException) //(TableTransactionFailedException ttfEx)
            {
                string lengthsInfo = "N/A";
                try
                {
                    lengthsInfo = string.Join(", ", tableEntities.Select(o => $"{o.PartitionKey}/{o.RowKey}: {JsonConvert.SerializeObject(o).Length}\n{JsonConvert.SerializeObject(o)}"));
                }
                catch { }

                var code = ex is TableTransactionFailedException ttfEx ? ttfEx.ErrorCode :
                    (ex is RequestFailedException rfEx ? rfEx.ErrorCode : null);
                throw new Exception($"{typeof(T).Name} code:{code} stored:{lengthsInfo}", ex);
            }
            catch
            {
                throw;
            }
        }

        public async Task<IEnumerable<T>> GetAll()
        {
            var filter = $"{nameof(ITableEntity.PartitionKey)} eq '{partitionKeyForFilter}'";
            var query = tableClient.QueryAsync<TTableEntity>(filter);
            var result = new List<T>();
            await foreach (var entity in query)
            {
                result.Add(toBusinessObject(entity));
                //var str = AzureTableConfig.GetLongString(entity);
            }
            return result;
        }

        public async Task<T?> Get(string id)
        {
            //var response = await tableClient.GetEntityIfExistsAsync<TTableEntity>(id.PartitionKey, id.RowKey);
            var response = await tableClient.GetEntityIfExistsAsync<TTableEntity>(partitionKeyForFilter, id);
            return response.HasValue ? toBusinessObject(response.Value) : default;
        }

        public async Task<string> Add(T item)
        {
            var entity = toTableEntity(item);
            await tableClient.AddEntityAsync(entity);
            //return new TableEntityId { PartitionKey = entity.PartitionKey, RowKey = entity.RowKey };
            return entity.RowKey;
        }

        public async Task Update(T item)
        {
            var entity = toTableEntity(item);
            await tableClient.UpdateEntityAsync(entity, ETag.All, TableUpdateMode.Replace);
        }

        public async Task Remove(T item)
        {
            var entity = toTableEntity(item);
            await tableClient.DeleteEntityAsync(entity.PartitionKey, entity.RowKey);
        }

        //public class TableEntityId
        //{
        //    public string PartitionKey { get; set; } = string.Empty;
        //    public string RowKey { get; set; } = string.Empty;
        //}
    }
}
