using Azure.Data.Tables;
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

        private string UuidToIdKey(string uuid)
        {
            var val = mnemoJapanese.ToIntWithRandom(uuid);
            if (val == null) throw new NullReferenceException($"uuid {uuid}");
            return val.Value.ToString().PadLeft(6, '0');
        }

        public async Task ModifyPartitionKeyUuidToId(string tableName)
        {
            Func<TableEntity, TableEntity> func = row => {
                //Console.WriteLine($"{row.PartitionKey} -> {UuidToIdKey(row.PartitionKey)}");
                row.PartitionKey = UuidToIdKey(row.PartitionKey);
                return row;
            };

            await ModifyEntities(new TableClient(connectionString, tableName), await CreateDestTable(tableName), func);
        }

        public async Task ModifyRowKeyUuidToId(string tableName)
        {
            Func<TableEntity, TableEntity> func = row => {
                row.RowKey = UuidToIdKey(row.RowKey);
                return row;
            };

            await ModifyEntities(new TableClient(connectionString, tableName), await CreateDestTable(tableName), func);
        }

        private async Task<TableClient?> CreateDestTable(string tableName)
        {
            if (connectionStringDst == null)
                return null;
            var tableNameDst = $"rekeyed{tableName}";
            var clientDst = new TableClient(connectionStringDst, tableNameDst);
            try
            {
                await clientDst.DeleteAsync();
            }
            catch { }
            await clientDst.CreateIfNotExistsAsync();
            return clientDst;
        }

        private async Task ModifyEntities(TableClient src, TableClient? dst, Func<TableEntity, TableEntity> modify)
        {
            var rows = src.QueryAsync<TableEntity>("", 100);
            await foreach (var page in rows.AsPages())
            {
                var transactions = page.Values.Select(row => new TableTransactionAction(TableTransactionActionType.Add, modify(row))).ToList();
                var byPartition = transactions.GroupBy(o => o.Entity.PartitionKey);
                foreach (var grp in byPartition)
                {
                    if (dst != null)
                    {
                        await dst.SubmitTransactionAsync(grp.ToList());
                    }
                }
            }
        }
    }
}
