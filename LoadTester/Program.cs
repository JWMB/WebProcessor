using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using static TrainingApi.Controllers.TrainingsController;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddUserSecrets<Program>()
    .Build();

var serviceProvider = new ServiceCollection().AddHttpClient().BuildServiceProvider();

var usernames = await GetUsernames(configuration["AdminApi:Group"]!);
usernames = usernames.Take(1);

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
var tasks = clients
    .Select((o, i) => new { Index = i, Client = o })
    .Select(item => item.Client.StartTraining(numDays: 5, pauseFactor: 1, initialPause: TimeSpan.FromSeconds(0.1 * item.Index)))
    .ToList();

await Task.WhenAll(tasks);

Console.WriteLine($"Done");

int GetTargetDay(int index, int maxIndex)
{
    var frac = 1f * index / maxIndex;
    return (int)(frac * 30) + 1;
}

async Task<IEnumerable<string>> GetUsernames(string group)
{
    using var client = new HttpClient();

    var summaries = await GetGroupTrainings(group);

    foreach (var trainingId in summaries.Select(o => o.Id))
    {
        var req = new HttpRequestMessage(HttpMethod.Delete, GetUri($"trainings?id={trainingId}"));
        AddAuth(req);
        var response = await client.SendAsync(req);
        response.EnsureSuccessStatusCode();
    }
    return summaries.Select(o => o.Username);

    async Task<List<TrainingSummaryWithDaysDto>> GetGroupTrainings(string group)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, GetUri($"Trainings/summaries?group={System.Net.WebUtility.UrlEncode(group)}"));
        AddAuth(req);
        var response = await client.SendAsync(req);
        response.EnsureSuccessStatusCode();
        var asString = await response.Content.ReadAsStringAsync();
        List<TrainingSummaryWithDaysDto>? result;
        try
        {
            result = System.Text.Json.JsonSerializer.Deserialize<List<TrainingSummaryWithDaysDto>>(asString, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }); // await response.Content.ReadFromJsonAsync<List<TrainingSummary>>();
        }
        catch (Exception ex)
        {
            throw ex;
        }
        if (result == null)
            throw new NullReferenceException();

        return result;
    }

    Uri GetUri(string path)
    {
        return new Uri(new Uri(configuration["AdminApi:BaseUrl"]!), path);
    }
    void AddAuth(HttpRequestMessage m)
    {
        var cookie = configuration["AdminApi:AuthCookie"];
        if (!string.IsNullOrEmpty(cookie))
            m.Headers.Add("Cookie", $".AspNetCore.Cookies={cookie}");
    }
}
