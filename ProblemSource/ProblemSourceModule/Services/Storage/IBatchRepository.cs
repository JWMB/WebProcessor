namespace ProblemSource.Services.Storage
{
    public interface IBatchRepository<T>
    {
        //void Add(T item);
        //void Add(IEnumerable<T> items);
        Task<IEnumerable<T>> GetAll();
        Task<(IEnumerable<T> Added, IEnumerable<T> Updated)> AddOrUpdate(IEnumerable<T> items);
    }

    public class InMemoryBatchRepository<T> : IBatchRepository<T>
    {
        private Func<T, string> idGenerator;
        private List<T> cached = new();

        public InMemoryBatchRepository(Func<T, string> idGenerator)
        {
            this.idGenerator = idGenerator;
        }

        public Task<(IEnumerable<T> Added, IEnumerable<T> Updated)> AddOrUpdate(IEnumerable<T> items)
        {
            var itemIds = items.Select(idGenerator).ToList();
            var existing = cached.Where(o => itemIds.Contains(idGenerator(o)));
            cached = cached.Except(existing).Concat(items).ToList();

            var existingIds = existing.Select(idGenerator).ToList();
            return Task.FromResult((items.Where(o => existingIds.Contains(idGenerator(o)) == false), existing));
        }

        public Task<IEnumerable<T>> GetAll() => Task.FromResult((IEnumerable<T>)cached);
    }

    //public class CachingUserAggregatesRepository<T> : IBatchRepository<T>
    //{
    //    private List<T> cached = new();
    //    private Func<T, string> idGenerator;
    //    private readonly IBatchRepository<T> storage;

    //    public CachingUserAggregatesRepository(IBatchRepository<T> storage, Func<T, string> idGenerator)
    //    {
    //        this.storage = storage;
    //        this.idGenerator = idGenerator;
    //    }

    //    public async Task<(IEnumerable<T> Added, IEnumerable<T> Updated)> AddOrUpdate(IEnumerable<T> items)
    //    {
    //        var result = await storage.AddOrUpdate(items);
    //        var updatedIds = result.Updated.Select(idGenerator).ToList();
    //        cached.RemoveAll(o => updatedIds.Contains(idGenerator(o)));
    //        cached.AddRange(result.Added);
    //        cached.AddRange(result.Updated);
    //        return result;
    //    }

    //    public async Task<IEnumerable<T>> GetAll()
    //    {
    //        cached = (await storage.GetAll()).ToList();
    //        return cached;
    //    }
    //}
}
