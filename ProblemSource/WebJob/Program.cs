using Common.Web;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProblemSource.Services.Storage.AzureTables;
using ProblemSourceModule.Services;
using WebJob;
using Microsoft.Azure.WebJobs.Logging.ApplicationInsights;

var builder = new HostBuilder();
builder.ConfigureWebJobs(b =>
{
    //b.AddAzureStorageCoreServices();
    //b.AddAzureStorage();
    //b.AddTimers();
})
.ConfigureAppConfiguration(b =>
{
    b.AddJsonFile("appsettings.json");
    //b.AddCommandLine(args);
})
.ConfigureLogging((context, b) =>
{
    if (context.Configuration["environment"] == Environments.Development)
    {
        b.SetMinimumLevel(LogLevel.Debug);
        b.AddConsole();
    }
    else
    {
        b.SetMinimumLevel(LogLevel.Information);
        b.AddConsole();
    }

    var aiConn = context.Configuration.GetValue<string>("ApplicationInsights:ConnectionString");
    if (!string.IsNullOrEmpty(aiConn))
        b.AddApplicationInsightsWebJobs(config => config.ConnectionString = aiConn);

    // configure CommonLogging to use Serilog
    //var logConfig = new LogConfiguration();
    //context.Configuration.GetSection("LogConfiguration").Bind(logConfig);
    //LogManager.Configure(logConfig);

    //var log = new LoggerConfiguration()
    //                          .WriteTo
    //                          .File("webjob-log.txt", rollingInterval: RollingInterval.Day)
    //                          .CreateLogger();
    //Log.Logger = log;
})
.ConfigureServices((context, services) =>
{
    services.AddSingleton(context.Configuration);
    services.AddSingleton<IWorkInstanceProvider, WorkInstanceProvider>();
    //services.AddMemoryCache();

    TypedConfiguration.ConfigureTypedConfiguration<AzureTableConfig>(services, context.Configuration, "AppSettings:AzureTable");
    //var section = context.Configuration.GetRequiredSection("AppSettings:AzureTable");
    //var tableConfig = TypedConfiguration.Bind<AzureTableConfig>(section);

    services.AddSingleton<TrainingAnalyzerCollection>();
    new ProblemSource.ProblemSourceModule().ConfigureForAzureTables(services, useCaching: false);
})
.UseConsoleLifetime();

using (var host = builder.Build())
{
    await host.StartAsync();

    var jobHost = host.Services.GetRequiredService<IJobHost>();
    await jobHost.CallAsync(nameof(Functions.ContinuousMethod));
}
