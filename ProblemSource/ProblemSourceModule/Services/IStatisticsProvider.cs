using ProblemSource.Models.Aggregates;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Models.Aggregates;

namespace ProblemSource.Services
{
    public interface IStatisticsProvider
    {
        Task<IEnumerable<TrainingDayAccount>> GetTrainingDays(int trainingId);
        Task<IEnumerable<PhaseStatistics>> GetPhaseStatistics(int trainingId);
        Task<IEnumerable<TrainingSummary?>> GetTrainingSummaries(IEnumerable<int> trainingIds);
    }

    public class StatisticsProvider : IStatisticsProvider
    {
        private readonly IUserGeneratedDataRepositoryProviderFactory userGeneratedDataRepositoryProviderFactory;

        public StatisticsProvider(IUserGeneratedDataRepositoryProviderFactory userGeneratedDataRepositoryProviderFactory)
        {
            this.userGeneratedDataRepositoryProviderFactory = userGeneratedDataRepositoryProviderFactory;
        }

        private IUserGeneratedDataRepositoryProvider GetDataProvider(int trainingId) =>
            userGeneratedDataRepositoryProviderFactory.Create(trainingId);

        public async Task<IEnumerable<PhaseStatistics>> GetPhaseStatistics(int trainingId) =>
            await GetDataProvider(trainingId).PhaseStatistics.GetAll();

        public async Task<IEnumerable<TrainingDayAccount>> GetTrainingDays(int trainingId) =>
            await GetDataProvider(trainingId).TrainingDays.GetAll();

        public async Task<IEnumerable<TrainingSummary?>> GetTrainingSummaries(IEnumerable<int> trainingIds)
        {
            var result = new List<TrainingSummary?>();
            foreach (var chunk in trainingIds.Chunk(10))
            {
                var tasks = chunk.Select(o => GetDataProvider(o).TrainingSummaries.GetAll());
                var resolved = await Task.WhenAll(tasks);
                result.AddRange(resolved.SelectMany(o => o));
            }
            return result;
        }
    }
}
