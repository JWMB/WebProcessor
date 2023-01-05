using Shouldly;
using ProblemSource.Services.Storage.AzureTables;
using ProblemSourceModule.Services.Storage;
using ProblemSourceModule.Services.Storage.AzureTables;
using System.Linq;

namespace ProblemSourceModule.Tests.AzureTable
{
    public class AzureTrainingRepositoryTests : AzureTableTestBase
    {
        [SkippableFact]
        public async Task AzureTableTrainingRepository_Add_Increment()
        {
            await Init();
            var repo = new AzureTableTrainingRepository(tableClientFactory);

            var idThatsIgnored = -999;
            var item = new Training { Id = idThatsIgnored, TrainingPlanName = "avc" };
            var newId = await repo.Add(item);

            item.Id.ShouldBe(newId);
            newId.ShouldBeGreaterThan(0);

            await repo.Remove(item);
        }

        private async Task ClearTable(AzureTableTrainingRepository repo)
        {
            var items = await repo.GetAll();
            foreach (var item in items)
                await repo.Remove(item);
        }

        [SkippableFact]
        public async Task AzureTableTrainingRepository_GetByIds()
        {
            await Init();
            var repo = new AzureTableTrainingRepository(tableClientFactory);

            var numToAdd = 3;
            var addedIds = new List<int>();

            foreach (var i in Enumerable.Range(0, numToAdd))
                addedIds.Add(await repo.Add(new Training { }));

            var expectedRetrievedIds = addedIds.Skip(1);
            var idsToGet = expectedRetrievedIds.Concat(new[] { 999 });

            var retrieved = await repo.GetByIds(idsToGet);
            retrieved.Select(o => o.Id).ShouldBe(expectedRetrievedIds, ignoreOrder: true);

            foreach (var id in addedIds)
                await repo.RemoveByIdIfExists(id);
        }
    }
}
