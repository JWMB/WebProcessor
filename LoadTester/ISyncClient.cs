using Microsoft.AspNetCore.Http.Json;
using ProblemSource.Models;
using System.Net.Http.Json;

public interface ISyncClient
{
    Task<SyncResult> Ping(SyncInput body);
    Task<SyncResult> Sync(SyncInput payload);
}

public class NullSyncClient : ISyncClient
{
    public Task<SyncResult> Sync(SyncInput payload) => Task.FromResult(new SyncResult());
    public Task<SyncResult> Ping(SyncInput payload) => Task.FromResult(new SyncResult());
}

public class SyncClient : ISyncClient
{
    public record Config(Uri Url, string Jwt);

    private readonly Config config;
    private readonly IHttpClientFactory clientFactory;
    public SyncClient(Config config, IHttpClientFactory clientFactory)
    {
        this.config = config;
        this.clientFactory = clientFactory;
    }
    public async Task<SyncResult> Ping(SyncInput payload) => await Send(payload, "sync/ping");

    public async Task<SyncResult> Sync(SyncInput payload) => await Send(payload, "sync/sync");

    private async Task<SyncResult> Send(SyncInput payload, string path)
    {
        using var client = clientFactory.CreateClient();

        var req = new HttpRequestMessage(HttpMethod.Post, new Uri(config.Url, path));
        req.Headers.Add("Authorization", "Bearer " + config.Jwt);

        req.Content = JsonContent.Create(payload, options: new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy .CamelCase });
        var response = await client.SendAsync(req);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"{response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var typed = System.Text.Json.JsonSerializer.Deserialize<SyncResult>(content);
        if (typed == null)
            throw new Exception("...");
        return typed;
    }
}
