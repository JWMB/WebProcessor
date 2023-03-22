using Common.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProblemSource;
using ProblemSource.Services.Storage;
using ProblemSource.Services.Storage.AzureTables;
using ProblemSourceModule.Services.Storage;
using ProblemSourceModule.Services.TrainingAnalyzers;
using Tools;

var config = CreateConfig();

//var azureTableSection = config.GetRequiredSection("AppSettings:AzureTable");
//var tableConfig = TypedConfiguration.Bind<AzureTableConfig>(azureTableSection);
var serviceProvider = InititalizeServices(config);

Console.WriteLine("Run tooling?");
if (!serviceProvider.GetRequiredService<AzureTableConfig>().ConnectionString.ToLower().Contains("usedevelopmentstorage"))
    Console.WriteLine("-----secrets.json has non-local settings - MAKE SURE THIS IS INTENDED!----");
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

var path = @"C:\Users\uzk446\Downloads\";

//await new OldDbMLFeatures().Run(cancellationToken);
//return;

//{
//    var trainingRepo = serviceProvider.GetRequiredService<ITrainingRepository>();

//    var ids = @"
//";
//    var factory = serviceProvider.GetRequiredService<IUserGeneratedDataRepositoryProviderFactory>();
//    var pathToModel = @"C:\Users\uzk446\source\repos\Trainer\WebProcessor\ProblemSource\ProblemSourceModule\Resources\JuliaMLModel_Reg.zip";
//    var analyzer = new CategorizerDay5_23Q1(new MLPredictNumberlineLevelService(new LocalMLPredictor(pathToModel)), serviceProvider.GetRequiredService<ILogger<CategorizerDay5_23Q1>>());

//    foreach (var id in ids.Split('\n').Select(o => o.Trim()).Where(o => o.Any()).Select(int.Parse))
//    {
//        var training = await trainingRepo.Get(id);
//        if (training == null)
//            throw new Exception("Training does not exist");
//        var dataProvider = factory.Create(training.Id);
//        var predicted = await analyzer.Predict(training, dataProvider);
//        if (predicted.PredictedPerformanceTier != PredictedNumberlineLevel.PerformanceTier.Unknown)
//        {
//            if (await analyzer.Analyze(training, dataProvider, null))
//            {
//                await trainingRepo.Update(training);
//            }
//        }
//    }
//}


//{
//    var copier = serviceProvider.CreateInstance<TrainingDataCopier>();

//    var srcTableConfig = serviceProvider.GetRequiredService<AzureTableConfig>();
//    //srcTableConfig.ConnectionString = "";
//    var srcProviderFactory = new AzureTableUserGeneratedDataRepositoriesProviderFactory(new TypedTableClientFactory(srcTableConfig));
//    var dstId = 10606; //server:10606 local:865640
//    var srcId = 2153;
//    await copier.CopyPhases(srcProviderFactory.Create(srcId), dstId, p => p.training_day <= 4, deleteInDst: p => true);
//}

//var tool = new TrainingStatsTools(serviceProvider);
//var result = await tool.GetTrainingIdsToTeachers(new[] { 9514, 9513, 9510, 936, 933, 91, 7857, 7821, 7205, 7204, 6109, 5949, 5447, 5019, 4990, 4957, 4933, 4356, 4111, 4050, 3931, 3523, 3507, 3413, 3396, 3342, 3340, 3338, 3336, 3320, 3315, 3312, 3309, 3156, 3081, 2442, 2435, 2425, 2423, 2421, 2278, 22760, 2203, 2202, 2201, 2198, 2197, 2195, 2193, 2182, 2144, 2142, 19068, 17908, 16266, 16236, 13835, 13834, 13830, 128, 126, 121, 120, 111, 10118, 10048, 100 });
//var oo = result.Select(o => new { Email = o.Key, Count = o.Value.SelectMany(p => p.Value).Count(), Value = o.Value }).OrderByDescending(o => o.Count).ToList();
//await tool.OverallStats(5);
//var result = await tool.GetUsersWithSyncedTrainings();


//var trainingMod = serviceProvider.CreateInstance<TrainingMod>();
//var ids = await trainingMod.GetTrainingsForTeacher("name@domain.se");
//var trainings = await (serviceProvider.GetRequiredService<ITrainingRepository>().GetByIds(ids));
//await trainingMod.ModifySettings(ids);

//var emails = BatchMail.ReadEmailFile(Path.Combine(path, "TeacherEmailsWithRejections.txt"));
//var emails = @"
//".Split('\n').Select(o => o.Trim().ToLower()).Where(o => o.Any());
//var creator = serviceProvider.CreateInstance<BatchCreateUsers>();
//await creator.CreateAndEmail(config, emails, true);


//var gmailService = BatchMail.CreateGmailService(config.GetRequiredSection("Gmail"));
//await BatchMail.SendBatch(gmailService, "Vektor invitation", emails, actuallySend: true); //Vektor - uppdatering


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
    IServiceCollection services = new ServiceCollection();
    services.AddSingleton(config);

    //TypedConfiguration.ConfigureTypedConfiguration<AzureTableConfig>(services, config, "AppSettings:AzureTable");
    var section = config.GetRequiredSection("AppSettings:AzureTable");
    //var tableConfig = TypedConfiguration.Bind<AzureTableConfig>(section);
    //services.AddSingleton(tableConfig);
    services.AddTransient(sp => TypedConfiguration.Bind<AzureTableConfig>(section));

    var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddSimpleConsole(); // i => i.ColorBehavior = LoggerColorBehavior.Disabled);
    });
    services.AddSingleton(loggerFactory);
    //services.AddSingleton(typeof(ILogger<>), sp => sp.GetRequiredService<ILoggerFactory>().CreateLogger<>());
    services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

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
