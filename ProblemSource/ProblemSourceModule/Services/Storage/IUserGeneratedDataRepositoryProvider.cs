using Microsoft.AspNetCore.DataProtection.KeyManagement;
using ProblemSource.Models;
using ProblemSource.Models.Aggregates;
using ProblemSourceModule.Models.Aggregates;
using ProblemSourceModule.Services.Storage;
using System.Collections.Concurrent;

namespace ProblemSource.Services.Storage
{
    public interface IUserGeneratedDataRepositoryProvider
    {
        IBatchRepository<Phase> Phases { get; }
        IBatchRepository<TrainingDayAccount> TrainingDays { get; }
        IBatchRepository<PhaseStatistics> PhaseStatistics { get; }

        IBatchRepository<TrainingSummary> TrainingSummaries { get; }
        IBatchRepository<UserGeneratedState> UserStates { get; }
    }

    //public interface IBatchAndRepository<T, TKey> : IBatchRepository<T>, IRepository<T, TKey>
    //{ }
    //public class CachingBatchRepositoryFacade<T, TKey> : IBatchAndRepository<T, TKey> //IBatchRepository<T>, IRepository<T, TKey>
    //{
    //  private readonly IBatchAndRepository<T, TKey> repo;
    //  public CachingBatchRepositoryFacade(IBatchAndRepository<T, TKey> repo)
    public class CachingBatchRepositoryFacade<T> : IBatchRepository<T>
    {
        private readonly IBatchRepository<T> repo;
        private readonly Func<T, string> createKey;
        private readonly ConcurrentDictionary<string, T> cached = new();

        public CachingBatchRepositoryFacade(IBatchRepository<T> repo, Func<T, string> createKey)
        {
            this.repo = repo;
            this.createKey = createKey;
        }

        //public async Task<TKey> Add(T item) => return await repo.Add(item);
        //public async Task<T?> Get(TKey id) => await repo.Get(id);
        //public async Task Remove(T item) => await repo.Remove(item);
        //public async Task Update(T item) => await repo.Update(item);
        //public Task<TKey> Upsert(T item) => throw new NotImplementedException();

        public Task<IEnumerable<T>> GetAll() => Task.FromResult((IEnumerable<T>)cached.Select(o => o.Value).ToList());

        public async Task<(IEnumerable<T> Added, IEnumerable<T> Updated)> Upsert(IEnumerable<T> items)
        {
            var result = await repo.Upsert(items);
            foreach (var item in result.Added)
                cached.TryAdd(createKey(item), item);

            foreach (var item in result.Updated)
                cached.TryUpdate(createKey(item), item, item);

            return result;
        }
    }
}
