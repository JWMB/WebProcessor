using Azure;
using ProblemSource.Services.Storage.AzureTables;
using ProblemSourceModule.Services.Storage.AzureTables;
using Shouldly;

namespace ProblemSourceModule.Tests.AzureTable
{
    public class AzureUserRepositoryTests : AzureTableTestBase
    {
        private readonly string fixedEmail = "unittester";


        private async Task<AzureTableUserRepository> InitRepo()
        {
            await Init();

            var repo = new AzureTableUserRepository(tableClientFactory);

            // clean up after any previously failed tests
            var retrieved = await repo.Get(fixedEmail);
            if (retrieved != null)
                await repo.Remove(retrieved);

            return repo;
        }

        [SkippableFact]
        public async Task UserRepository_AddConflict()
        {
            var repo = await InitRepo();
            var user1 = new Services.Storage.User { Email = fixedEmail, Role = "Admin", Trainings = new List<int> { 1, 2, 3 } };

            await repo.Add(user1);
            Should.Throw<RequestFailedException>(() => repo.Add(user1));

            await repo.Remove(user1);
        }

        [SkippableFact]
        public async Task UserRepository_AddGetEquivalent()
        {
            var repo = await InitRepo();

            var user1 = new Services.Storage.User { Email = fixedEmail, Role = "Admin", Trainings = new List<int> { 1, 2, 3 } };
            await repo.Add(user1);

            var retrieved = await repo.Get(user1.Email);

            retrieved.ShouldBeEquivalentTo(user1);

            await repo.Remove(user1);
        }
    }
}
