using Azure.Data.Tables;
using System.Collections.Concurrent;
using ProblemSource.Models;

namespace ProblemSource.Services
{
    public interface IUserStateRepository
    {
        Task Set(string uuid, object state); //IUserGeneratedState
        Task<object?> Get(string uuid); //IUserGeneratedState
        Task<T?> Get<T>(string uuid) where T : class; //IUserGeneratedState
    }

    public class InMemoryUserStateRepository : IUserStateRepository
    {
        private static ConcurrentDictionary<string, object> userStates = new ConcurrentDictionary<string, object>();
        //private static ConcurrentDictionary<string, IUserGeneratedState> userStates = new ConcurrentDictionary<string, IUserGeneratedState>();
        public async Task Set(string uuid, object state) //IUserGeneratedState
        {
            userStates.AddOrUpdate(uuid, state, (s1, s2) => state);
            await System.IO.File.WriteAllTextAsync(@"", Newtonsoft.Json.JsonConvert.SerializeObject(state));
        }

        public async Task<object?> Get(string uuid) //IUserGeneratedState
        {
            return userStates.GetOrAdd(uuid, s => {
                return new UserGeneratedState() { };
            });
        }
        public Task<T?> Get<T>(string uuid) where T : class => throw new NotImplementedException();
    }

    public class AzureTableConfig
    {
        //public string StorageUri { get; set; }
        //public string? AccountName { get; set; }
        //public string? StorageAccountKey { get; set; }

        public string ConnectionString { get; set; } = "";
        public string TableUserStates { get; set; } = "";
        public string TableUserLogs { get; set; } = "";
        public string TableTrainingPlans { get; set; } = "";

        //public TableSharedKeyCredential CreateCredentials() =>
        //    new TableSharedKeyCredential(AccountName, StorageAccountKey);
        //public TableServiceClient CreateServiceClient() =>
        //    new TableServiceClient(new Uri(StorageUri), CreateCredentials());
        //public TableClient CreateTableClient(string tableName) =>
        //    new TableClient(new Uri(StorageUri), tableName, CreateCredentials());

        public TableServiceClient CreateServiceClient() =>
            new TableServiceClient(ConnectionString); // new Uri(StorageUri));

        public TableClient CreateTableClient(string tableName) =>
            new TableClient(ConnectionString, tableName); // new Uri(StorageUri), tableName, CreateCredentials());

        public static void SetLongString(TableEntity entity, string str, string prefix = "Data")
        {
            var max = 32 * 1024;
            for (int i = 0; i < (int)Math.Ceiling((decimal)str.Length / max); i++)
            {
                var index = i * max;
                entity.Add($"{prefix}{i}", str.Substring(index, Math.Min(max, str.Length - index)));
            }
        }
        public static string GetLongString(TableEntity entity, string prefix = "Data")
        {
            var sb = new System.Text.StringBuilder();
            int i = 0;
            while (true)
            {
                if (entity.TryGetValue($"{prefix}{i++}", out var obj))
                    sb.Append(obj.ToString());
                else
                    break;
            }
            return sb.ToString();
        }
    }

    public class AzureTableUserStateRepository : IUserStateRepository
    {
        private readonly AzureTableConfig config;
        private readonly string partitionKey = "p";

        public AzureTableUserStateRepository(AzureTableConfig config)
        {
            this.config = config;
        }

        public async Task<object?> Get(string uuid) //IUserGeneratedState
        {
            var tableClient = config.CreateTableClient(config.TableUserStates);
            await tableClient.CreateIfNotExistsAsync();
            var queryResultsFilter = tableClient.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{partitionKey}' and RowKey eq '{uuid}'");

            await foreach (var entity in queryResultsFilter)
            {
                var str = AzureTableConfig.GetLongString(entity);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(str); //UserFullState
            }

            return null;
        }

        public async Task<T?> Get<T>(string uuid) where T : class
        {
            var obj = await Get(uuid);
            return obj == null ? null : Newtonsoft.Json.JsonConvert.DeserializeObject<T>(Newtonsoft.Json.JsonConvert.SerializeObject(obj));
        }


        public async Task Set(string uuid, object state) //IUserGeneratedState
        {
            var entity = new TableEntity(partitionKey, uuid);
            AzureTableConfig.SetLongString(entity, Newtonsoft.Json.JsonConvert.SerializeObject(state));
            ////var json = Newtonsoft.Json.JsonConvert.SerializeObject(obj);

            //var json = ;
            //var max = 32 * 1024;
            //for (int i = 0; i < (int)Math.Ceiling((decimal)json.Length / max); i++)
            //{
            //    var index = i * max;
            //    entity.Add($"Data{i}", json.Substring(index, Math.Min(max, json.Length - index)));
            //}
            await config.CreateTableClient(config.TableUserStates).UpsertEntityAsync(entity, TableUpdateMode.Replace);
        }
    }
}
