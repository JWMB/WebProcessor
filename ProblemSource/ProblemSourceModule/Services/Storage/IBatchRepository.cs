namespace ProblemSource.Services.Storage
{
    public interface IBatchRepository<T>
    {
        Task<IEnumerable<T>> GetAll();
        Task<(IEnumerable<T> Added, IEnumerable<T> Updated)> Upsert(IEnumerable<T> items);
    }

    public class InMemoryBatchRepository<T> : IBatchRepository<T>
    {
        private Func<T, string> idGenerator;
        private List<T> cached = new();

        public InMemoryBatchRepository(Func<T, string> idGenerator)
        {
            this.idGenerator = idGenerator;
        }

        public Task<(IEnumerable<T> Added, IEnumerable<T> Updated)> Upsert(IEnumerable<T> items)
        {
            var itemIds = items.Select(idGenerator).ToList();
            var existing = cached.Where(o => itemIds.Contains(idGenerator(o)));
            cached = cached.Except(existing).Concat(items).ToList();

            var existingIds = existing.Select(idGenerator).ToList();
            return Task.FromResult((items.Where(o => existingIds.Contains(idGenerator(o)) == false), existing));
        }

        public Task<IEnumerable<T>> GetAll() => Task.FromResult((IEnumerable<T>)cached);
    }
}
