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
            var user1 = new Models.User { Email = fixedEmail, Role = "Admin", Trainings = new Dictionary<string, List<int>> { { "", new List<int> { 1, 2, 3 } } } };

            await repo.Add(user1);
            Should.Throw<RequestFailedException>(() => repo.Add(user1));

            await repo.Remove(user1);
        }

        [SkippableFact]
        public async Task UserRepository_AddGetEquivalent()
        {
            var repo = await InitRepo();

            var user1 = new Models.User { Email = fixedEmail, Role = "Admin", Trainings = new Dictionary<string, List<int>> { { "", new List<int> { 1, 2, 3 } } } };
            await repo.Add(user1);

            var retrieved = await repo.Get(user1.Email);

            if (retrieved == null) throw new NullReferenceException(nameof(retrieved));
            // https://github.com/shouldly/shouldly/issues/767 - nested Dictiotionary not comparable
            // TODO: retrieved.ShouldBeEquivalentTo(user1);
            retrieved!.Trainings.ShouldBeEquivalentTo(user1.Trainings);
            new { retrieved.Email, retrieved.HashedPassword, retrieved.Role }.ShouldBeEquivalentTo(new { user1.Email, user1.HashedPassword, user1.Role });

            await repo.Remove(user1);
        }
    }

    public static class ShouldlyLikeExtensions
    {
        public static void ShouldBeEquivalentTo<TKey, TValue>(this IDictionary<TKey, TValue> a, IDictionary<TKey, TValue> b)
        {
            a.Keys.ShouldBe(b.Keys, ignoreOrder: true);
            foreach (var kv in a)
            {
                kv.Value.ShouldBeEquivalentTo(b[kv.Key]);
            }
        }
    }
}
