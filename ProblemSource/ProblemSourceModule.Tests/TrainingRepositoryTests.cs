using Shouldly;
using ProblemSource.Services.Storage.AzureTables;
using ProblemSourceModule.Services.Storage;
using ProblemSourceModule.Services.Storage.AzureTables;

namespace ProblemSourceModule.Tests
{
    public class TrainingRepositoryTests
    {
        public TrainingRepositoryTests()
        {
            Skip.If(!System.Diagnostics.Debugger.IsAttached);
        }

        [SkippableFact]
        public async Task AzureTableTrainingRepository_Add_Increment()
        {
            var tableFactory = await TypedTableClientFactory.Create(new AzureTableConfig { ConnectionString = "" });

            ITrainingRepository repo = new AzureTableTrainingRepository(tableFactory);

            var newId = await repo.Add(new Training { Id = 0, TrainingPlanName = "avc" });
            newId.ShouldBeGreaterThan(0);
        }
    }
}
