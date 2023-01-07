using Azure.Data.Tables;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Primitives;
using ProblemSource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tools
{
    internal class MigrateAzureTableColumn
    {
        private readonly string connectionString;
        private readonly string? connectionStringDst;
        private readonly MnemoJapanese mnemoJapanese;

        // PartitionKey uuid: vektorPhases vektorPhaseStatistics vektorTrainingDays vektorUserLogs
        // RowKey uuid: vektorUserStates
        // RowKey id: vektorTrainings

        public MigrateAzureTableColumn(string connectionString, string? connectionStringDst = null)
        {
            this.connectionString = connectionString;
            this.connectionStringDst = connectionStringDst;
            mnemoJapanese = new MnemoJapanese(2);
        }

        private static readonly string[] tablesWithUuidInPartitionKey = "vektor vektorPhases vektorPhaseStatistics vektorTrainingDays vektorUserLogs".Split(' ');
        private static readonly string[] tablesWithUuidInRowKey = "vektorUserStates vektorTrainings".Split(' ');
        private static readonly string[] tablesOthers = "vektorUsers".Split(' ');
        public static readonly string RekeyedPrefix = "rekeyed";

        public static async Task RenameAll(string connectionString, string srcPrefix, string dstPrefix = "")
        {
            var allTables = tablesWithUuidInPartitionKey.Concat(tablesWithUuidInRowKey).Concat(tablesOthers);
            foreach (var table in allTables)
            {
                await RenameTable(connectionString, $"{srcPrefix}{table}", $"{dstPrefix}{table}");
            }
        }

        public async Task MigrateAll()
        {
            foreach (var table in tablesWithUuidInPartitionKey)
                await Copy(table, ModifyPartitionKey);

            foreach (var table in tablesWithUuidInRowKey)
                await Copy(table, ModifyRowKey);

            foreach (var table in tablesOthers)
                await Copy(table);

            async Task Copy(string table, Func<TableEntity, TableEntity>? modify = null)
            {
                var src = new TableClient(connectionString, table);
                if (await TableExists(src))
                    await CopyEntities(new TableClient(connectionString, table), await CreateDestTable(table), modify);
            }
        }

        private string UuidToIdKey(string uuid)
        {
            int? val;
            if (int.TryParse(uuid.TrimStart('0'), out var parsed))
                val = parsed;
            else
                val = mnemoJapanese.ToIntWithRandom(uuid);

            if (val == null) throw new NullReferenceException($"uuid {uuid}");
            return val.Value.ToString().PadLeft(6, '0');
        }

        private TableEntity ModifyPartitionKey(TableEntity row)
        {
            //Console.WriteLine($"{row.PartitionKey} -> {UuidToIdKey(row.PartitionKey)}");
            row.PartitionKey = UuidToIdKey(row.PartitionKey);
            return row;
        }
        private TableEntity ModifyRowKey(TableEntity row)
        {
            row.RowKey = UuidToIdKey(row.RowKey);
            return row;
        }

        public static async Task RenameTable(string connectionString, string nameSrc, string nameDst)
        {
            if (nameSrc == nameDst) throw new Exception("dst same as src");

            var clientSrc = new TableClient(connectionString, nameSrc);
            if (await TableExists(clientSrc))
            {
                var clientDst = await RecreateTable(connectionString, nameDst);
                await CopyEntities(clientSrc, clientDst);

                await clientSrc.DeleteAsync();
            }
        }

        private static async Task<bool> TableExists(TableClient client)
        {
            try
            {
                await client.GetAccessPoliciesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static async Task<TableClient> RecreateTable(string connectionString, string tableName)
        {
            var client = new TableClient(connectionString, tableName);
            try
            {
                await client.DeleteAsync();
            }
            catch { }

            // Delete operation may not be completed although task is complete...
            for(int i = 0; i < 10; i++)
            {
                try
                {
                    await Task.Delay(500 + (int)Math.Pow(1.3, i) * 1000);
                    await client.CreateAsync();
                    break;
                }
                catch (Azure.RequestFailedException rfEx) when(rfEx.ErrorCode == "TableBeingDeleted")
                {
                    Console.WriteLine($"{tableName} is being deleted ({i})"); // when this happens, it often succeeds when i = 8
                }
                catch (Azure.RequestFailedException rfEx) when (rfEx.ErrorCode == "TableAlreadyExists")
                {
                    Console.WriteLine($"{tableName} already exists ({i})");
                    break;
                }
            }
            return client;
        }

        private async Task<TableClient?> CreateDestTable(string tableName)
        {
            if (connectionStringDst == null)
                return null;
            var tableNameDst = $"{RekeyedPrefix}{tableName}";
            var clientDst = await RecreateTable(connectionStringDst, tableNameDst);
            return clientDst;
        }

        private static async Task CopyEntities(TableClient src, TableClient? dst, Func<TableEntity, TableEntity>? modify = null)
        {
            Console.WriteLine($"Start {src.Name}");

            var rows = src.QueryAsync<TableEntity>("", 100);
            var cnt = 0;
            await foreach (var page in rows.AsPages())
            {
                var transactions = page.Values.Select(row => new TableTransactionAction(TableTransactionActionType.Add, modify == null ? row : modify(row))).ToList();
                var byPartition = transactions.GroupBy(o => o.Entity.PartitionKey);
                foreach (var grp in byPartition)
                {
                    if (dst != null)
                    {
                        await dst.SubmitTransactionAsync(grp.ToList());
                    }
                }
                var lastCnt = cnt;
                cnt += page.Values.Count;
                if (Math.Floor(1.0 * lastCnt / 1000) < Math.Floor(1.0 * cnt / 1000))
                    Console.WriteLine($"{cnt}");
            }
        }
    }
}
