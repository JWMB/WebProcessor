using Common.Web;
using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddUserSecrets<Program>();

var config = builder.Build();

Console.WriteLine("Run tooling?");
if (Console.ReadKey().Key != ConsoleKey.Y)
{
    Console.WriteLine("kbye");
    return;
}

var section = config.GetRequiredSection("AppSettings:AzureTable");
var tableConfig = TypedConfiguration.Bind<ProblemSource.Services.Storage.AzureTables.AzureTableConfig>(section);
var dbTools = new OldDbAdapter.Tools(tableConfig);
//var byGroupName = await dbTools.GetTeachersWithTrainings(20, 15);
//var withMostTrainings = byGroupName.OrderByDescending(o => o.Value.Count()).First();
await dbTools.MoveTeacherAndTrainingsToAzureTables(29158);

//await dbTools.MoveToAzureTables(withMostTrainings.Value.Take(10));

//var connStr = config.GetOrThrow<string>("AppSettings:AzureTable:ConnectionString");
////var migrator = new MigrateAzureTableColumn(connStr, "UseDevelopmentStorage=true");
//var migrator = new MigrateAzureTableColumn(connStr, connStr);
//await migrator.MigrateAll();
////await MigrateAzureTableColumn.RenameAll("UseDevelopmentStorage=true", MigrateAzureTableColumn.RekeyedPrefix, "aaa");

Console.WriteLine("Done");
