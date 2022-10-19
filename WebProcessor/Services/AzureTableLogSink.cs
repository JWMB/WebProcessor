using Azure.Data.Tables;
using PluginModuleBase;
using ProblemSource.Services.Storage.AzureTables;

namespace WebApi.Services
{
    public class AzureTableLogSink : IDataSink
    {
        private readonly TableClient client;

        public AzureTableLogSink(AzureTableConfig config)
        {
            client = config.CreateTableClient(config.TableUserLogs);
        }

        public async Task Log(string uuid, object data)
        {
            var entity = new TableEntity(uuid, ((long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0,0,0,0, DateTimeKind.Utc)).TotalMilliseconds).ToString());
            AzureTableConfig.SetLongString(entity, Newtonsoft.Json.JsonConvert.SerializeObject(data));

            await client.CreateIfNotExistsAsync();
            await client.AddEntityAsync(entity);
        }
    }
}
