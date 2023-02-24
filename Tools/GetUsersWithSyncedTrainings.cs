using ProblemSource;
using ProblemSource.Services;
using ProblemSourceModule.Models.Aggregates;
using ProblemSourceModule.Services.Storage.AzureTables;

namespace Tools
{
    internal class GetUsersWithSyncedTrainings
    {
        public async Task<List<string>> Run(IServiceProvider serviceProvider)
        {
            var userRepo = serviceProvider.CreateInstance<AzureTableUserRepository>();
            var users = await userRepo.GetAll();
            var userToTrainings = users.Where(o => o.Role == "Teacher").ToDictionary(o => o.Email, o => o.Trainings.SelectMany(p => p.Value));
            var trainingToUsers = userToTrainings.SelectMany(o => o.Value.Select(p => new { TrainingId = p, User = o.Key }))
                .GroupBy(o => o.TrainingId).Select(o => new { TrainingId = o.Key, Users = o.Select(p => p.User).ToList() })
                .ToDictionary(o => o.TrainingId, o => o.Users);
            ;

            var allTrainingsToCheck = userToTrainings.Values.SelectMany(o => o).ToList();

            var statisticsProvider = serviceProvider.CreateInstance<StatisticsProvider>();
            var summaries = (await statisticsProvider.GetTrainingSummaries(allTrainingsToCheck)).OfType<TrainingSummary>();

            var summaryToUser = summaries.Select(o => new { Summary = o, Users = trainingToUsers[o.Id] }).ToList();
            var allUsersWithSummaries = summaryToUser.SelectMany(o => o.Users).Distinct().ToList();

            return allUsersWithSummaries;
        }
    }
}
