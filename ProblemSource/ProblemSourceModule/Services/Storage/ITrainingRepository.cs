using ProblemSource.Services.Storage;
using ProblemSource;
using ProblemSourceModule.Models;
using ProblemSource.Models;

namespace ProblemSourceModule.Services.Storage
{

    public interface ITrainingRepository : IRepository<Training, int>
    {
        Task<IEnumerable<Training>> GetByIds(IEnumerable<int> ids);

        async Task<int> Add(ITrainingUsernameService usernameService, Training training)
        {
            var id = await Add(training);

            training.Username = usernameService.FromId(id);
            await Update(training);
            return training.Id;
        }
    }
}
