using ProblemSource.Services.Storage;
using ProblemSource;
using ProblemSourceModule.Models;
using ProblemSource.Models;

namespace ProblemSourceModule.Services.Storage
{

    public interface ITrainingRepository : IRepository<Training, int>
    {
        Task<IEnumerable<Training>> GetByIds(IEnumerable<int> ids);

        async Task<Training> Add(ITrainingPlanRepository trainingPlanRepository, ITrainingUsernameService usernameService, string trainingPlan, TrainingSettings? settings, string? ageBracket = null)
        {
            var tp = await trainingPlanRepository.Get(trainingPlan);
            if (tp == null)
                throw new Exception($"Training plan not found: {trainingPlan}");

            var training = new Training
            {
                TrainingPlanName = trainingPlan,
                Settings = settings ?? TrainingSettings.Default,
                AgeBracket = ageBracket ?? "",
                Created = DateTimeOffset.UtcNow,
            };

            var id = await Add(training);

            training.Username = usernameService.FromId(id);
            await Update(training);
            return training;
        }
    }
}
