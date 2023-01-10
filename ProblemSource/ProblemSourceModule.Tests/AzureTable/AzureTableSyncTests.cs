using Common;
using ProblemSource.Models;
using ProblemSource.Models.LogItems;

namespace ProblemSourceModule.Tests.AzureTable
{
    public class AzureTableSyncTests : AzureTableTestBase
    {
        [SkippableFact]
        public async Task X()
        {
            //await Init();

            var dir = Directory.GetCurrentDirectory();
            var json = await File.ReadAllTextAsync(@"AzureTable\training1054598.json");

            var jsonItems = System.Text.Json.JsonSerializer.Deserialize<List<LogItem>>(json);
            if (jsonItems == null) throw new NullReferenceException();

            var logItems = jsonItems.Select(o => LogItem.TryDeserialize(o).parsed).Cast<LogItem>();
            var splitByDay = logItems.SplitBy(o => o is EndOfDayLogItem).ToList();

            foreach (var dayItems in splitByDay)
            {

            }
        }
    }
}
