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
}
