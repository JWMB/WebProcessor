﻿// See https://aka.ms/new-console-template for more information
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebJob;

var builder = new HostBuilder();
builder.ConfigureWebJobs(b =>
{
    //b.AddAzureStorageCoreServices();
    //b.AddAzureStorage();
    //b.AddTimers();
})
.ConfigureAppConfiguration(b =>
{
    // Adding command line as additional configuration source
    //b.AddCommandLine(args);
})
.ConfigureLogging((context, b) =>
{
    // here we can access context.HostingEnvironment.IsDevelopment() yet
    if (context.Configuration["environment"] == EnvironmentName.Development)
    {
        //b.SetMinimumLevel(LogLevel.Debug);
        //b.AddConsole();
    }
    else
    {
        //b.SetMinimumLevel(LogLevel.Information);
    }

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
    //services.AddMemoryCache();

    // other DI configuration here
})
.UseConsoleLifetime();

//var host = builder.Build();
//Services = host.Services;

using (var host = builder.Build())
{
    //await host.RunAsync();

    await host.StartAsync();

    var jobHost = host.Services.GetService<IJobHost>();
    await jobHost.CallAsync(nameof(Functions.MyContinuousMethod));
}
