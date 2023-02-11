using Common.Web;
using EmailServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ProblemSource.Services.Storage.AzureTables;
using Tools;

var config = CreateConfig();

Console.WriteLine("Run tooling?");
Console.WriteLine("-----MAKE SURE secrets.json IS NOT INADVERTEDLY USING PRODUCTION SETTINGS!----");
if (Console.ReadKey().Key != ConsoleKey.Y)
{
    Console.WriteLine("kbye");
    return;
}

var section = config.GetRequiredSection("AppSettings:AzureTable");
var tableConfig = TypedConfiguration.Bind<AzureTableConfig>(section);

var serviceProvider = InititalizeServices(config);

//await serviceProvider.CreateInstance<BatchCreateUsers>().CreateAndEmail(config, actuallyCreate: true);

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

IConfigurationRoot CreateConfig()
{
    return new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
        .AddUserSecrets<Program>()
        .Build();
}

IServiceProvider InititalizeServices(IConfigurationRoot config)
{
    var section = config.GetRequiredSection("AppSettings:AzureTable");
    var tableConfig = TypedConfiguration.Bind<AzureTableConfig>(section);

    IServiceCollection services = new ServiceCollection();
    services.AddSingleton(config);
    services.AddSingleton(tableConfig);
    var module = new ProblemSource.ProblemSourceModule();
    module.ConfigureServices(services);
    var serviceProvider = services.BuildServiceProvider();
    module.Configure(new App(serviceProvider));
    return serviceProvider;
}

class App : IApplicationBuilder
{
    private IServiceProvider sp;

    public App(IServiceProvider sp)
    {
        this.sp = sp;
    }
    public IServiceProvider ApplicationServices { get => sp; set => sp = value; }
    public IFeatureCollection ServerFeatures => throw new NotImplementedException();
    public IDictionary<string, object?> Properties => throw new NotImplementedException();
    public RequestDelegate Build() => throw new NotImplementedException();
    public IApplicationBuilder New() => throw new NotImplementedException();
    public IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware) => throw new NotImplementedException();
}
