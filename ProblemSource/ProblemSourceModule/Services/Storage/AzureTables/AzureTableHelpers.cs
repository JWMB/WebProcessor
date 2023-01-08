using Azure.Data.Tables;

namespace ProblemSourceModule.Services.Storage.AzureTables
{
    public class AzureTableHelpers
    {
        public static async Task IterateOverRows(TableClient client, string filter, Func<TableEntity, TableTransactionAction> createTransaction, Func<string, IEnumerable<TableTransactionAction>, Task> executeTransaction)
        {
            var rows = client.QueryAsync<TableEntity>(filter, 100);
            var cnt = 0;
            await foreach (var page in rows.AsPages())
            {
                var transactions = page.Values.Select(createTransaction).ToList();
                var byPartition = transactions.GroupBy(o => o.Entity.PartitionKey);
                foreach (var grp in byPartition)
                {
                    await executeTransaction(grp.Key, grp);
                }
                var lastCnt = cnt;
                cnt += page.Values.Count;
            }
        }
    }
}
