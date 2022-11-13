using Shouldly;
using ProblemSource.Models.Aggregates;
using ProblemSource.Services.Storage.AzureTables.TableEntities;
using Common;
using ProblemSource.Services.Storage.AzureTables;
using ProblemSourceModule.Services.Storage;
using ProblemSourceModule.Services.Storage.AzureTables;
using AutoFixture.AutoMoq;
using AutoFixture;

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
            var tableFactory = await TableClientFactory.Create(null);

            ITrainingRepository repo = new AzureTableTrainingRepository(tableFactory);

            var newId = await repo.Add(new Training { Id = 0, TrainingPlanName = "avc" });
            newId.ShouldBeGreaterThan(0);
        }
    }
}
