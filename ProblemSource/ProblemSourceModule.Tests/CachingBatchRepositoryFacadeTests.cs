using Microsoft.Extensions.Caching.Memory;
using ProblemSource.Services.Storage;
using Shouldly;

namespace ProblemSourceModule.Tests
{
    public class CachingBatchRepositoryFacadeTests
    {
        [Fact]
        public async Task CachingBatchRepository_Added_Updated()
        {
            var memoryCache = new MemoryCache(Microsoft.Extensions.Options.Options.Create(new MemoryCacheOptions { }));
            var repo = new InMemoryBatchRepository<Item>(o => $"id={o}");
            var cachingRepo = new CachingBatchRepositoryFacade<Item>(memoryCache, repo, "prefix", o => $"{o.Id}");

            var result = await cachingRepo.Upsert(new[] { new Item { Id = 1, Name = "a" }, new Item { Id = 2, Name = "b" } });
            result.Added.Count().ShouldBe(2);

            result = await cachingRepo.Upsert(new[] { new Item { Id = 1, Name = "newa" }, new Item { Id = 3, Name = "b" } });
            result.Added.Single().Id.ShouldBe(3);
            result.Updated.Single().Id.ShouldBe(1);
            result.Updated.Single().Name.ShouldBe("newa");

            var all = await cachingRepo.GetAll();
            all.Count().ShouldBe(3);
        }

        [Fact]
        public async Task CachingBatchRepository_Seeding()
        {
            var memoryCache = new MemoryCache(Microsoft.Extensions.Options.Options.Create(new MemoryCacheOptions { }));

            var repo = new InMemoryBatchRepository<Item>(o => $"id={o}");
            var cachingRepo = new CachingBatchRepositoryFacade<Item>(memoryCache, repo, "prefix", o => $"{o.Id}");

            await repo.Upsert(new[] { new Item { Id = 1, Name = "a" }, new Item { Id = 2, Name = "b" } });

            var all = await cachingRepo.GetAll();
            all.Count().ShouldBe(2);

            // Instantiate again, same cache is used so same result
            repo = new InMemoryBatchRepository<Item>(o => $"id={o}");
            cachingRepo = new CachingBatchRepositoryFacade<Item>(memoryCache, repo, "prefix", o => $"{o.Id}");

            all = await cachingRepo.GetAll();
            all.Count().ShouldBe(2);
        }

        public class Item
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }
}
