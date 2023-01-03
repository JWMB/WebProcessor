
using Microsoft.Extensions.Configuration;
using Tools;

var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddUserSecrets<Program>();

var config = builder.Build();

var connStr = config["AppSettings:AzureTable:ConnectionString"];
var migrator = new MigrateAzureTableColumn(connStr, "UseDevelopmentStorage=true");

await migrator.MigrateAll();
