using Azure.Data.Tables;
using PluginModuleBase;

namespace Common.Web.Services
{
    public class AzureTableLogSink : IDataSink
    {
        private readonly TableClient client;
        private bool inited = false;

        public AzureTableLogSink(ITableClientFactory factory)
        {
            client = factory.CreateClient("");
        }

        public async Task Log(string uuid, object data)
        {
            if (!inited)
            {
                await client.CreateIfNotExistsAsync();
                inited = true;
            }

            // TODO: use ExpandableTableEntityConverter instead
            var entity = new TableEntity(uuid, ((long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds).ToString());
            AzureTableHelpers.SetLongString(entity, System.Text.Json.JsonSerializer.Serialize(data));

            await client.CreateIfNotExistsAsync();
            await client.AddEntityAsync(entity);
        }
    }
}
