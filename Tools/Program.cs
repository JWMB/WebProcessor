
using Common;
using Microsoft.Extensions.Configuration;
using Tools;

var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddUserSecrets<Program>();

var config = builder.Build();


Console.WriteLine("Migrate Azure tables?");
if (Console.ReadKey().Key != ConsoleKey.Y)
{
    Console.WriteLine("kbye");
    return;
}

var connStr = config.GetOrThrow<string>("AppSettings:AzureTable:ConnectionString");
//var migrator = new MigrateAzureTableColumn(connStr, "UseDevelopmentStorage=true");
var migrator = new MigrateAzureTableColumn(connStr);
await migrator.MigrateAll();

//await MigrateAzureTableColumn.RenameAll("UseDevelopmentStorage=true", MigrateAzureTableColumn.RekeyedPrefix, "aaa");
