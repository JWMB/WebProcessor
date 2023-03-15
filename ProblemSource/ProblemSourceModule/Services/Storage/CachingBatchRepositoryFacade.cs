using Microsoft.Extensions.Caching.Memory;

namespace ProblemSource.Services.Storage
{
    //public interface IBatchAndRepository<T, TKey> : IBatchRepository<T>, IRepository<T, TKey>
    //{ }
    //public class CachingBatchRepositoryFacade<T, TKey> : IBatchAndRepository<T, TKey> //IBatchRepository<T>, IRepository<T, TKey>
    //{
    //  private readonly IBatchAndRepository<T, TKey> repo;
    //  public CachingBatchRepositoryFacade(IBatchAndRepository<T, TKey> repo)
    public class CachingBatchRepositoryFacade<T> : IBatchRepository<T>
    {
        private readonly IBatchRepository<T> repo;
        private readonly Func<T, string> createKeySuffix;
        private readonly string cacheKeyPrefix;
        private readonly IMemoryCache cache;
        //private bool isSeeded = false;

        public CachingBatchRepositoryFacade(IMemoryCache cache, IBatchRepository<T> repo, string cacheKeyPrefix, Func<T, string> createKeySuffix)
        {
            this.cache = cache;
            this.repo = repo;
            this.createKeySuffix = createKeySuffix;
            this.cacheKeyPrefix = cacheKeyPrefix;
        }

        //public async Task<TKey> Add(T item) => return await repo.Add(item);
        //public async Task<T?> Get(TKey id) => await repo.Get(id);
        //public async Task Remove(T item) => await repo.Remove(item);
        //public async Task Update(T item) => await repo.Update(item);
        //public Task<TKey> Upsert(T item) => throw new NotImplementedException();

        private async Task SeedCache()
        {
            // Don't return early - let sliding expiration be updated
            //if (isSeeded)
            //    return;
            var isSeededKey = $"_{cacheKeyPrefix}_seeded";
            if (cache.Get<bool?>(isSeededKey) == true)
                return;
            var all = await repo.GetAll();
            foreach (var item in all)
                cache.Set(CreateKey(item), item, CreateCacheOptions());
            //isSeeded = true;
            cache.Set(isSeededKey, true, CreateCacheOptions(decreasedExpiration: true));

        }

        private MemoryCacheEntryOptions CreateCacheOptions(bool decreasedExpiration = false)
        {
            return new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(10 - (decreasedExpiration ? 1 : 0)) };
        }

        public async Task<IEnumerable<T>> GetAll()
        {
            await SeedCache();
            return GetCachedByKeys(GetCachedKeys());
        }

        private IEnumerable<T> GetCachedByKeys(IEnumerable<string> keys)
        {
            if (!keys.Any())
                return Enumerable.Empty<T>();
            var items = keys.Select(cache.Get<T>).OfType<T>().ToList();
            return items;
        }

        private string CreateKey(T item) => $"{cacheKeyPrefix}{createKeySuffix(item)}";

        private List<string> GetCachedKeys() => cache.GetKeys<string>().Where(o => o.StartsWith(cacheKeyPrefix)).ToList();

        public async Task<(IEnumerable<T> Added, IEnumerable<T> Updated)> Upsert(IEnumerable<T> items)
        {
            await SeedCache();

            var cachedKeys = GetCachedKeys();
            var lookupByKey = items.ToDictionary(CreateKey, o => o);
            //var result = await repo.Upsert(items);
            //foreach (var item in result.Added)
            //    cached.TryAdd(createKey(item), item);
            //foreach (var item in result.Updated)
            //    cached.TryUpdate(createKey(item), item, item);
            //return result;

            // TODO: this is a workaround for Azure table upsert not responding with updated or inserted

            var added = lookupByKey.ExceptBy(cachedKeys, o => o.Key);
            await repo.Upsert(added.Select(o => o.Value));

            var updated = lookupByKey.IntersectBy(cachedKeys, o => o.Key);
            // These might be identical - check with cached values before committing
            // TODO: IEquatable<>?
            var forCompare = GetCachedByKeys(updated.Select(o => o.Key)).ToDictionary(CreateKey, o => o);
            var modified = new List<T>();
            foreach (var item in updated)
            {
                var comp = forCompare[item.Key];
                if (comp?.Equals(item.Value) == false)
                {
                    modified.Add(item.Value);
                }
            }
            await repo.Upsert(modified);

            foreach (var kv in lookupByKey)
                cache.Set(kv.Key, kv.Value, CreateCacheOptions());

            return (added.Select(o => o.Value).ToList(), updated.Select(o => o.Value).ToList());

            //var lookupByKey = items.ToDictionary(o => createKey(o), o => o);
            //var toUpdate = lookupByKey.IntersectBy(cached.Keys, o => o.Key);
            //foreach (var kv in toUpdate)
            //    cached.TryUpdate(kv.Key, kv.Value, kv.Value);

            //var toAdd = lookupByKey.ExceptBy(cached.Keys, o => o.Key);
            //foreach (var kv in toAdd)
            //    cached.TryAdd(kv.Key, kv.Value);

            //return (toAdd.Select(o => o.Value).ToList(), toUpdate.Select(o => o.Value).ToList());
        }

        public async Task<int> RemoveAll()
        {
            var cacheKeys = GetCachedKeys();
            foreach (var key in cacheKeys)
                cache.Remove(key);

            var count = await repo.RemoveAll();
            return count;
        }
    }
}
