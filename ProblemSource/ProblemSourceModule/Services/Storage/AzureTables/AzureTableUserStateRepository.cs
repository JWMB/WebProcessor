using Azure.Data.Tables;
using ProblemSource.Services.Storage.AzureTables.TableEntities;
using ProblemSourceModule.Services.Storage;
using System;

namespace ProblemSource.Services.Storage.AzureTables
{
    public class AzureTableUserStateRepository : IUserStateRepository
    {
        private readonly ITableClientFactory tableClientFactory;
        //private readonly ExpandableTableEntityConverter<Training> converter;
        //private readonly TableEntityRepository<object, TableEntity> repo;
        private readonly string staticPartitionKey = "none";

        public AzureTableUserStateRepository(ITableClientFactory tableClientFactory)
        {
            this.tableClientFactory = tableClientFactory;

            //converter = new ExpandableTableEntityConverter<Training>(t => (staticPartitionKey, t.Id.ToString()));
            //repo = new TableEntityRepository<Training, TableEntity>(tableClientFactory.UserStates, converter.ToPoco, converter.FromPoco, staticPartitionKey);
        }

        public async Task<object?> Get(string uuid)
        {
            var tableClient = tableClientFactory.UserStates;
            var queryResultsFilter = tableClient.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{staticPartitionKey}' and RowKey eq '{uuid}'");

            await foreach (var entity in queryResultsFilter)
            {
                var str = AzureTableConfig.GetLongString(entity);
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
            AzureTableConfig.SetLongString(entity, Newtonsoft.Json.JsonConvert.SerializeObject(state));

            await tableClientFactory.UserStates.UpsertEntityAsync(entity, TableUpdateMode.Replace);
        }
    }
}
