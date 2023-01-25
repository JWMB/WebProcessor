using Microsoft.Extensions.DependencyInjection;
using ProblemSourceModule.Models;
using System.Diagnostics;
using System.Net.Http.Json;
using TestClient;

// TODO: Clean and pre-generate some some trainings to day 20

// Max syncs per second seems to have been ~1.4 (see README). Below code locally with 1 training reaches 10 syncs/sec
var apiBaseUrl = "https://localhost:7173/api/";

var trainingIds = new[] { 2 };
var trainingUsernames = new List<string>();

{
    var c = new HttpClient();
    // Note: on localhost, we can bypass authorization by providing /swagger/ referer
    c.DefaultRequestHeaders.Referrer = new Uri($"{apiBaseUrl}swagger/");

    foreach (var id in trainingIds)
    {
        var trainingsUrl = $"{apiBaseUrl}trainings/";
        //var xxx = await c.GetAsync($"{trainingsUrl}{id}");
        var training = await c.GetFromJsonAsync<Training>($"{trainingsUrl}{id}");
        if (training != null)
        {
            trainingUsernames.Add(training.Username);

            var response = await c.DeleteAsync($"{trainingsUrl}?id={id}&deleteTrainingDataOnly=true");
        }
        else
        {

        }
    }
}

IServiceCollection services = new ServiceCollection();
services.AddHttpClient();

var serviceProvider = services.BuildServiceProvider();

var syncService = new SyncService(serviceProvider.GetRequiredService<IHttpClientFactory>());
await syncService.Init(apiBaseUrl);

var stopwatch = new Stopwatch();

// init clients
var clients = trainingUsernames.Select(id => new Client(syncService, id)).ToList();
stopwatch.Start();
foreach (var client in clients)
{
    // Init gets state - slower than subsequent syncs
    await client.Init();
}
stopwatch.Stop();
var initClientsPerSecond = 1M * clients.Count / stopwatch.ElapsedMilliseconds;

// TODO: Seed clients so they're at different training days (i.e. different amounts of data to load/cache)
// TODO: add endpoint for clearing cache so we can simulate "fresh sync" (or maybe any sync with EndOfDayLogItem should trigger session/cache clearing)?


// Start regular syncing:
stopwatch.Start();
var numSyncs = 0;
while (true)
{
    foreach (var client in clients)
    {
        client.GenerateNextPhase();
        await client.Sync();
        numSyncs++;
    }
    if (numSyncs > 100)
        break;
}
stopwatch.Stop();
var syncsPerSecond = 1.0 * numSyncs / stopwatch.Elapsed.TotalSeconds;
Console.WriteLine($"syncs: {numSyncs}\tsyncsPerSecond: {syncsPerSecond}");

Console.ReadKey();
