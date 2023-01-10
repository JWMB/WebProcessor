using Azure;
using Azure.Data.Tables;
using Newtonsoft.Json;
using ProblemSource.Services.Storage.AzureTables.TableEntities;
using ProblemSourceModule.Services.Storage;
using System.Text.RegularExpressions;

namespace ProblemSource.Services.Storage.AzureTables
{
    public class AutoConvertTableEntityRepository<T> : TableEntityRepository<T, TableEntity> where T : class, new()
    {
        public AutoConvertTableEntityRepository(TableClient tableClient, ExpandableTableEntityConverter<T> converter, TableFilter keyFilter)
            : base(tableClient, converter.ToPoco, converter.FromPoco, keyFilter)
        {
        }
    }


    public class TableFilter
    {
        public TableFilter(string partition, string? row = null)
        {
            Partition = partition;
            Row = row;
        }

        public string Partition { get; private set; }
        public string? Row { get; private set; }

        public string Render() => 
            RenderPartitionOnly()
            + (string.IsNullOrEmpty(Row) ? "" : $" and {nameof(ITableEntity.RowKey)} eq '{Row}'");

        public string RenderPartitionOnly() => $"{nameof(ITableEntity.PartitionKey)} eq '{Partition}'";
    }

    public class TableEntityRepository<T, TTableEntity> : IRepository<T, string>, IBatchRepository<T> where TTableEntity : class, ITableEntity, new()
    {
        protected readonly TableClient tableClient;
        private readonly Func<TTableEntity, T> toBusinessObject;
        private readonly Func<T, TTableEntity> toTableEntity;
        private readonly TableFilter keyForFilter;

        public TableEntityRepository(TableClient tableClient, Func<TTableEntity, T> toBusinessObject, Func<T, TTableEntity> toTableEntity, TableFilter keyForFilter)
        {
            this.tableClient = tableClient;
            this.toBusinessObject = toBusinessObject;
            this.toTableEntity = toTableEntity;
            this.keyForFilter = keyForFilter;
        }

        private async Task<List<Response>> UpsertBatch(IEnumerable<ITableEntity> entities)
        {
            var batch = new List<TableTransactionAction>(entities.Select(f => new TableTransactionAction(TableTransactionActionType.UpsertMerge, f)));

            // TODO: the 100 limit makes it no longer a transaction >:(
            var result = new List<Response>();
            foreach (var chunk in batch.Chunk(100))
            {
                var response = await tableClient.SubmitTransactionAsync(chunk);
                if (response.Value.Any(o => o.IsError))
                    throw new Exception($"SubmitTransaction errors: {string.Join("\n", response.Value.Where(o => o.IsError).Select(o => o.ReasonPhrase))}");
                result.AddRange(response.Value);
            }

            return result;
        }

        // https://learn.microsoft.com/en-us/rest/api/storageservices/Understanding-the-Table-Service-Data-Model#characters-disallowed-in-key-fields
        public static Regex InvalidKeyRegex = new Regex(@"\/|\\|#|\?");

        public async Task<(IEnumerable<T> Added, IEnumerable<T> Updated)> Upsert(IEnumerable<T> items)
        {
            if (!items.Any())
                return (new List<T>(), new List<T>());

            var tableEntities = items.Select(toTableEntity).ToList();

            var duplicates = tableEntities.GroupBy(o => $"{o.PartitionKey}__{o.RowKey}").Where(o => o.Count() > 1).ToList();
            if (duplicates.Any())
            {
                throw new Exception($"{typeof(T).Name}: Duplicate entries: {string.Join(",", duplicates.Select(o => $"{o.First().PartitionKey}/{o.First().RowKey}"))}");
            }

            var invalidEntries = tableEntities.Where(o => InvalidKeyRegex.IsMatch(o.RowKey) || InvalidKeyRegex.IsMatch(o.PartitionKey));
            if (invalidEntries.Any())
            {
                throw new Exception($"{typeof(T).Name}: Invalid key(s) for {string.Join(",", invalidEntries.Select(o => $"{o.PartitionKey}/{o.RowKey}"))}");
            }

            try
            {
                var responses = await UpsertBatch(tableEntities);

                // TODO: do we always get 204 with TableTransactionActionType.UpsertMerge ?
                // would UpsertReplace always get 201? Goddamn azure tables...
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

        public async Task<Dictionary<string, T?>> GetByRowKeys(IEnumerable<string> rowKeys, string? partitionKey = null)
        {
            var results = new List<KeyValuePair<string, T?>>();
            var result = new Dictionary<string, T?>();
            foreach (var chunk in rowKeys.Chunk(50))
            {
                var tasks = chunk.Select(o => tableClient.GetEntityIfExistsAsync<TTableEntity>(partitionKey ?? keyForFilter.Partition, o));
                var partial = await Task.WhenAll(tasks);
                var kvs = partial.Select((o, i) => new KeyValuePair<string, T?>(chunk[i], o.HasValue ? toBusinessObject(o.Value) : default(T)));  //KeyValuePair<string, T?>.Create(i, o.HasValue ? toBusinessObject(o.Value) : null));
                results.AddRange(kvs);
            }
            return results.ToDictionary(o => o.Key, o => o.Value);
        }

        public async Task<IEnumerable<T>> GetAll()
        {
            return await GetWithFilter(keyForFilter.Render());
        }

        private async Task<IEnumerable<T>> GetWithFilter(string filter)
        {
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
            var response = await tableClient.GetEntityIfExistsAsync<TTableEntity>(keyForFilter.Partition, id);
            return response.HasValue ? toBusinessObject(response.Value) : default;
        }

        public async Task<string> Add(T item)
        {
            var entity = toTableEntity(item);
            await tableClient.AddEntityAsync(entity);
            return entity.RowKey;
        }

        public async Task<string> Upsert(T item)
        {
            var entity = toTableEntity(item);
            await tableClient.UpsertEntityAsync(entity);
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

        public static Exception ReduceException(Exception exception)
        {
            if (exception is AggregateException aEx && aEx.InnerExceptions.Count == 1)
            {
                return aEx.InnerExceptions.First();
            }
            return exception;
        }

        //public class TableEntityId
        //{
        //    public string PartitionKey { get; set; } = string.Empty;
        //    public string RowKey { get; set; } = string.Empty;
        //}
    }
}
