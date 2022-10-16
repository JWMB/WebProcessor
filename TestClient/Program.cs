using TestClient;

var syncService = new SyncService();
await syncService.Init();

var clientUserIds = new[] { "ab1" }; //, "ab2", "ab3" 

foreach (var userId in clientUserIds)
{
    var client = new Client(syncService, userId);
    await client.Init();
    await client.PlayDay(1);
}

Console.ReadKey();
