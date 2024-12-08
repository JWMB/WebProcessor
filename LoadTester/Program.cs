using LoadTester;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddUserSecrets<Program>()
    .Build();

var serviceProvider = new ServiceCollection().AddHttpClient().BuildServiceProvider();

var admin = new AdminClient(configuration.GetSection("AdminApi").Get<AdminClient.Config>()!);
var usernames = await admin.GetUsernames(configuration["AdminApi:Group"]!);
//usernames = usernames.Take(1);

var clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
var syncClient = new SyncClient(configuration.GetSection("SyncClient").Get<SyncClient.Config>()!, clientFactory);
var clients = usernames.Select(userId => new TrainingClient(userId, syncClient)).ToList();

// first move trainings quickly to distributed days
var initTasks = clients
    .Select((o, i) => new { Index = i, Client = o, StartDay = GetTargetDay(i, clients.Count) })
    .Select(item => item.Client.StartTraining(untilDay: item.StartDay, skipRegularSync: true, pauseFactor: 0))
    .ToList();

await Task.WhenAll(initTasks);

// here we start the actual load test
var rnd = new Random();
var minSecsBetweenStarts = 0.01;
var tasks = clients
    .Select((o, i) => new { Index = i, Client = o })
    .Select(item => item.Client.StartTraining(numDays: 5, pauseFactor: 0.1f, initialPause: TimeSpan.FromSeconds(minSecsBetweenStarts * (rnd.NextDouble() + item.Index))))
    .ToList();

await Task.WhenAll(tasks);

Console.WriteLine($"Done");

int GetTargetDay(int index, int maxIndex)
{
    var frac = 1f * index / maxIndex;
    return (int)(frac * 30) + 1;
}
