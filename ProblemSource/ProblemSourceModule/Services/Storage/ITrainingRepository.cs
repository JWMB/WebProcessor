using ProblemSourceModule.Models;

namespace ProblemSourceModule.Services.Storage
{

    public interface ITrainingRepository : IRepository<Training, int>, IAddGetId<Training, int>
	{
        Task<IEnumerable<Training>> GetByIds(IEnumerable<int> ids);

        async Task<int> Add(ITrainingUsernameService usernameService, Training training)
        {
            var id = await AddGetId(training);

            training.Username = usernameService.FromId(id);
            await Update(training);
            return training.Id;
        }
    }
}
