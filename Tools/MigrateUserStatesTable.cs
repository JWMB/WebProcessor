using Azure.Data.Tables;
using Common.Web;
using Newtonsoft.Json.Linq;
using ProblemSource.Models;
using ProblemSource.Services.Storage.AzureTables;

namespace Tools
{
    internal class MigrateUserStatesTable
    {
        public static async Task Run(AzureTableConfig tableConfig)
        {
            var tableClientFactory = new TypedTableClientFactory(tableConfig);

            //var client = new TableClient(tableConfig.ConnectionString, $"{tableConfig.TablePrefix}UserStates");
            var q = tableClientFactory.UserStates.QueryAsync<TableEntity>("");
            var currentRows = new Dictionary<string, UserGeneratedState>();
            await foreach (var entity in q)
            {
                var str = AzureTableHelpers.GetLongString(entity);
                if (string.IsNullOrEmpty(str))
                    continue;

                //var deserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(str);
                var deserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<UserGeneratedState>(str);
                if (deserialized == null)
                    throw new Exception("null!");
                if (deserialized.exercise_stats.gameRuns.Any() == false)
                    throw new Exception("no game runs!");
                currentRows.Add(entity.RowKey, deserialized);
            }

            foreach (var kv in currentRows)
            {
                var prov = new AzureTableUserGeneratedDataRepositoryProvider(tableClientFactory, int.Parse(kv.Key));
                await prov.UserStates.Upsert(new[] { kv.Value });
            }
        }
    }
}
