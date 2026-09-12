namespace ProblemSourceModule.Services.Storage
{
    public interface IRepository<TEntity, TId>
    {
        Task<IEnumerable<TEntity>> GetAll();
        Task<TEntity?> Get(TId id);
        Task Add(TEntity item);
		//Task<TId> Upsert(TEntity item);
		Task Upsert(TEntity item);
		Task Update(TEntity item);
        Task Remove(TEntity item);
        //Task Remove(TId id);
    }
	public interface IAddGetId<TEntity, TId>
    {
		Task<TId> AddGetId(TEntity item);

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
