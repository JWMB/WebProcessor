// See https://aka.ms/new-console-template for more information

using TestClient;

var syncService = new SyncService();
await syncService.Init();

var clientIds = new[] { "ab1" }; //, "ab2", "ab3" 

foreach (var item in clientIds)
{
    var client = new Client(syncService, "abc");
    await client.Init();
    await client.PlayDay(1);
}

Console.ReadKey();
