using ProblemSourceModule.Models;

namespace ProblemSourceModule.Services.Storage
{

    public interface ITrainingRepository : IRepository<Training, int>
    {
        Task<IEnumerable<Training>> GetByIds(IEnumerable<int> ids);
    }
}
