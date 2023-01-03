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

        public async Task MigrateAll()
        {
            var tables = "vektor vektorPhases vektorPhaseStatistics vektorTrainingDays vektorUserLogs".Split(' ');
            //var tables = "vektorTrainingDays".Split(' ');
            foreach (var table in tables)
            {
                await ModifyPartitionKeyUuidToId(table);
            }

            tables = "vektorUserStates vektorTrainings".Split(' ');
            foreach (var table in tables)
            {
                await ModifyRowKeyUuidToId(table);
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
            Console.WriteLine($"Start {src.Name}");

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
