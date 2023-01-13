using Azure.Data.Tables;

namespace ProblemSource.Services.Storage.AzureTables
{
    public class AzureTableConfig
    {
        public string ConnectionString { get; set; } = "";
        public string TablePrefix { get; set; } = "";
        public string TableUserLogs { get; set; } = "";
        public string TableTrainingPlans { get; set; } = "";

        public TableClient CreateTableClient(string tableName) => new TableClient(ConnectionString, tableName);

        public static string IdToKey(int id) => id.ToString().PadLeft(6, '0');
        public static int KeyToId(string key) => int.Parse(key);
    }
}
