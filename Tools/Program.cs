using Common.Web;
using Microsoft.Extensions.Configuration;
using ProblemSource.Services.Storage.AzureTables;
using Tools;

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
var tableConfig = TypedConfiguration.Bind<AzureTableConfig>(section);

//await TrainingMod.ModifySettings(tableConfig);
//await MigrateUserStatesTable.Run(tableConfig);

//var dbTools = new OldDbAdapter.Tools(tableConfig);
////var byGroupName = await dbTools.GetTeachersWithTrainings(20, 15);
////var withMostTrainings = byGroupName.OrderByDescending(o => o.Value.Count()).First();
////await dbTools.MoveTeacherAndTrainingsToAzureTables(29158, true);
//var items = await dbTools.CreateLogFromOldTraining(1054598);
//var goodJson = "[" + string.Join(",\n  ", items.Select(o => $"{Newtonsoft.Json.JsonConvert.SerializeObject(o)}")) + "]";

//var connStr = tableConfig.ConnectionString; // config.GetOrThrow<string>("AppSettings:AzureTable:ConnectionString");
////var migrator = new MigrateAzureTableColumn(connStr, "UseDevelopmentStorage=true");
//var migrator = new MigrateAzureTableColumn(connStr, connStr);
//await migrator.MigrateAll();
////await MigrateAzureTableColumn.RenameAll("UseDevelopmentStorage=true", MigrateAzureTableColumn.RekeyedPrefix, "aaa");

Console.WriteLine("Done");
