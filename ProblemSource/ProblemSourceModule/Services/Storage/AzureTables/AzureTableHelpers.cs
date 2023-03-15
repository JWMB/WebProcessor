using Azure;
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

    public static class AzureTableExtensions
    {
        public static async Task<List<Response>> SubmitTransactionsBatched(this TableClient tableClient, IEnumerable<TableTransactionAction> transactions)
        {
            // TODO: the 100 limit makes it no longer a transaction >:(
            var result = new List<Response>();
            foreach (var chunk in transactions.Chunk(100))
            {
                var response = await tableClient.SubmitTransactionAsync(chunk); // Not supported type System.Collections.Generic.Dictionary`2[System.String,System.Int32]
                if (response.Value.Any(o => o.IsError))
                    throw new Exception($"SubmitTransaction errors: {string.Join("\n", response.Value.Where(o => o.IsError).Select(o => o.ReasonPhrase))}");
                result.AddRange(response.Value);
            }
            return result;
        }
    }
}
