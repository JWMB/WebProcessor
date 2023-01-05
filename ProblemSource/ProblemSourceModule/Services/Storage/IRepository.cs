namespace ProblemSourceModule.Services.Storage
{
    public interface IRepository<TEntity, TId>
    {
        //IEnumerable<TEntity> Query();
        Task<IEnumerable<TEntity>> GetAll();
        Task<TEntity?> Get(TId id);
        Task<TId> Add(TEntity item);
        Task Update(TEntity item);
        Task Remove(TEntity item);
        //Task Remove(TId id);
    }

    public static class IRepositoryExtensions
    {
        public static async Task<bool> RemoveByIdIfExists<TEntity, TId>(this IRepository<TEntity, TId> repo, TId id)
        {
            var found = await repo.Get(id);
            if (found != null)
                await repo.Remove(found);
            return found != null;
        }
    }
}
