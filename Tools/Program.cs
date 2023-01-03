
using Microsoft.Extensions.Configuration;
using Tools;

var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddUserSecrets<Program>();

var config = builder.Build();

var connStr = config["AppSettings:AzureTable:ConnectionString"];
var migrator = new MigrateAzureTableColumn(connStr, "UseDevelopmentStorage=true");

var tables = "vektor vektorPhases vektorPhaseStatistics vektorTrainingDays vektorUserLogs".Split(' ');
//var tables = "vektorTrainingDays".Split(' ');
foreach (var table in tables)
{
    await migrator.ModifyPartitionKeyUuidToId(table);
}

tables = "vektorUserStates".Split(' ');
foreach (var table in tables)
{
    await migrator.ModifyRowKeyUuidToId(table);
}
