using Azure.Data.Tables;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Tools
{
    internal class FixAzureTableQuotedDateTime
    {
        private readonly string connectionString;

        public FixAzureTableQuotedDateTime(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task Fix(Dictionary<string, List<(string, Type)>> tablesAndColumns, string? filter)
        {
            foreach (var item in tablesAndColumns)
            {
                await Fix(item.Key, item.Value, filter);
            }
        }

        public async Task Fix(string table, IEnumerable<(string Name, Type Type)> columns, string? filter)
        {
            
            var client = new TableClient(connectionString, table);
            var query = client.QueryAsync<TableEntity>(filter);
            //var result = new List<TableEntity>();
            var modifiedList = new List<string>();
            await foreach (var entity in query)
            {
                var modified = false;
                foreach (var col in columns)
                {
                    var colName = col.Name;
                    var value = entity[colName];
                    if (value != null)
                    {
                        if (value is string str)
                        {
                            modified = true;
                            if (str == "null")
                            {
                                entity[colName] = null;
                            }
                            else if (str.StartsWith("\""))
                            {
                                if (col.Type == typeof(DateTime))
                                {
                                    entity[colName] = DateTime.Parse(str.Trim('"'));
                                }
                                else if (col.Type == typeof(DateTimeOffset))
                                {
                                    entity[colName] = DateTimeOffset.Parse(str.Trim('"'));
                                }
                                else
                                { }
                            }
                            else
                            {
                                modified = false;
                            }
                        }
                        else
                        { }
                    }
                }
                if (modified)
                {
                    modifiedList.Add($"{entity.PartitionKey}.{entity.RowKey}");
                    //await client.UpdateEntityAsync(entity, entity.ETag);
                }
                //result.Add(entity);
            }
            //return result;
        }
    }
}
