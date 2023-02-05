using Common.Web;
using EmailServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ProblemSource.Services.Storage.AzureTables;
using System.IO;
using Tools;

var config = CreateConfig();

Console.WriteLine("Run tooling?");
Console.WriteLine("-----MAKE SURE secrets.json IS NOT INADVERTEDLY USING PRODUCTION SETTINGS!----");
if (Console.ReadKey().Key != ConsoleKey.Y)
{
    Console.WriteLine("kbye");
    return;
}

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) =>
{
    Console.WriteLine("Canceling...");
    cts.Cancel();
    e.Cancel = true;
};
var cancellationToken = cts.Token;

//var path = @"C:\Users\uzk446\Downloads\";
//var ml = new MLDynamic();
//await ml.Train(new[] { Path.Join(path, "taxi-fare-train.csv"), Path.Join(path, "taxi-fare-test.csv") },
//    new MLDynamic.ColumnInfo { Label = "fare_amount", Categorical = new[] { "rate_code", "vendor_id", "payment_type" } },
//    Path.Join(path, "taxi-fare-model.zip"), TimeSpan.FromMinutes(10));
//var val = ml.Predict(new
//{
//    vendor_id = "CMT",
//    rate_code = 1,
//    passenger_count = 1,
//    trip_time_in_secs = 1271,
//    trip_distance = 3.8f,
//    payment_type = "CRD",
//    fare_amount = 0 //17.5
//});

await new OldDbMLFeatures().Run(cancellationToken);
return;

var section = config.GetRequiredSection("AppSettings:AzureTable");
var tableConfig = TypedConfiguration.Bind<AzureTableConfig>(section);

var serviceProvider = InititalizeServices(config);


var emails = new[] { "jonas.beckeman@outlook.com" }; // "jonas.beckeman@outlook.com" //var emails = File.ReadAllLines("").Where(o => o.Length > 0);
var createdUsersInfo = BatchCreateUsers.CreateDummyUserList(emails);
//var createdUsersInfo = await serviceProvider.CreateInstance<BatchCreateUsers>().CreateUsers(emails, new Dictionary<string, int> { { "Test", 10 } }, "2018 VT template Default");
File.WriteAllText("createdUsers.json", JsonConvert.SerializeObject(createdUsersInfo));
await BatchMail.SendInvitations(config, createdUsersInfo);

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
    // TODO: we probably want some of these in a central place, as it's used by several applications
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

public static class IServiceProviderExtensions
{
    public static T CreateInstance<T>(this IServiceProvider instance) where T : class
    {
        var constructors = typeof(T).GetConstructors();

        var constructor = constructors.First();
        var parameterInfo = constructor.GetParameters();

        var parameters = parameterInfo.Select(o => instance.GetRequiredService(o.ParameterType)).ToArray();

        return (T)constructor.Invoke(parameters);
    }
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
