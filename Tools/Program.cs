using Common.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ProblemSource.Services;
using ProblemSource.Services.Storage.AzureTables;
using Tools;
using static ProblemSource.Services.BatchCreateUsers;
using static ProblemSource.Services.LogEventsToPhases;

var config = CreateConfig();

Console.WriteLine("Run tooling?");
if (Console.ReadKey().Key != ConsoleKey.Y)
{
    Console.WriteLine("kbye");
    return;
}

var section = config.GetRequiredSection("AppSettings:AzureTable");
var tableConfig = TypedConfiguration.Bind<AzureTableConfig>(section);

var serviceProvider = InititalizeServices(config);

var creator = serviceProvider.CreateInstance<BatchCreateUsers>();
//var emails = new[] { "giuliag.lazzaro@gmail.com" };
var emails = new[] { "torkel.klingberg@gmail.com", "torkel.klingberg@ki.se" };
//var emails = new[] { "jonas.beckeman@outlook.com" };
//var emails = File.ReadAllLines("").Where(o => o.Length > 0);

//var result = await creator.CreateUsers(emails, new Dictionary<string, int> { { "Test", 1 } });
//File.WriteAllText("createdUsers.json", JsonConvert.SerializeObject(result));
var result = emails.Select(email => new CreateUserResult { User = new ProblemSourceModule.Models.User { Email = email }, WasCreated = true, Password = "bla blabla", CreatedTrainings = new Dictionary<string, List<string>> { { "Test", new() { "ajaj fofo", "nubbe sddfd" } } } });

var batchMail = new BatchMail(BatchMail.CreateGmailService(config.GetRequiredSection("Gmail")));
foreach (var item in result.Where(o => o.WasCreated))
{
    var msg = BatchMail.CreateNewUserCreatedMessage(item);
    msg.From = new System.Net.Mail.MailAddress("jonas.beckeman@gmail.com");
    var mime = GmailService.CreateMimeMessage(msg);

    batchMail.Send(msg);
}
var str = string.Join("\n", result.Select(o => $"--User: {o.User.Email}\tPassword: {o.Password}\n{o.CreatedTrainingsToString()}"));
Console.WriteLine(str);

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
