using Microsoft.Extensions.DependencyInjection;


var serviceProvider = new ServiceCollection().AddHttpClient().BuildServiceProvider();
var clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

var numClients = 100;

var clients = Enumerable.Range(0, numClients).Select(o => new TrainingClient($"{o}", clientFactory)).ToList();

var tasks = clients
    .Select((o, i) => new { Index = i, Client = o })
    .Select(item => item.Client.PerformOneDaysTraining(TimeSpan.FromSeconds(0.1 * item.Index)))
    .ToList();

await Task.WhenAll(tasks);
