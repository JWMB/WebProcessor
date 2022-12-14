using Azure.Data.Tables;
using Common.Web;
using System;

namespace ProblemSource.Services.Storage.AzureTables
{
    public class AzureTableUserStateRepository : IUserStateRepository
    {
        private readonly ITypedTableClientFactory tableClientFactory;
        //private readonly ExpandableTableEntityConverter<Training> converter;
        //private readonly TableEntityRepository<object, TableEntity> repo;
        private readonly string staticPartitionKey = "none";

        public AzureTableUserStateRepository(ITypedTableClientFactory tableClientFactory)
        {
            this.tableClientFactory = tableClientFactory;

            //converter = new ExpandableTableEntityConverter<Training>(t => (staticPartitionKey, t.Id.ToString()));
            //repo = new TableEntityRepository<Training, TableEntity>(tableClientFactory.UserStates, converter.ToPoco, converter.FromPoco, staticPartitionKey);
        }

        public async Task<List<string>> GetUuids()
        {
            // TODO: only needed until we switch to accountId as rowKeys
            var tableClient = tableClientFactory.UserStates;
            var queryResultsFilter = tableClient.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{staticPartitionKey}'");
            var uuids = new List<string>();
            await foreach (var entity in queryResultsFilter)
            {
                uuids.Add(entity.RowKey);
            }
            return uuids;
        }

        public async Task<object?> Get(string uuid)
        {
            var tableClient = tableClientFactory.UserStates;
            var queryResultsFilter = tableClient.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{staticPartitionKey}' and RowKey eq '{uuid}'");

            await foreach (var entity in queryResultsFilter)
            {
                var str = AzureTableHelpers.GetLongString(entity);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(str);
            }

            return null;
        }

        public async Task<T?> Get<T>(string uuid) where T : class
        {
            var obj = await Get(uuid);
            return obj == null ? null : Newtonsoft.Json.JsonConvert.DeserializeObject<T>(Newtonsoft.Json.JsonConvert.SerializeObject(obj));
        }

        public async Task Set(string uuid, object state)
        {
            var entity = new TableEntity(staticPartitionKey, uuid);
            AzureTableHelpers.SetLongString(entity, Newtonsoft.Json.JsonConvert.SerializeObject(state));

            await tableClientFactory.UserStates.UpsertEntityAsync(entity, TableUpdateMode.Replace);
        }
    }
}
