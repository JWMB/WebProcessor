using ProblemSource.Models.Aggregates;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Models.Aggregates;
using ProblemSourceModule.Services.Storage;

namespace ProblemSource.Services
{
    public interface IStatisticsProvider
    {
        Task<IEnumerable<TrainingDayAccount>> GetTrainingDays(int trainingId);
        Task<IEnumerable<PhaseStatistics>> GetPhaseStatistics(int trainingId);
        Task<IEnumerable<TrainingSummary?>> GetTrainingSummaries(IEnumerable<int> trainingIds);
        Task<List<TrainingSummary>> GetAllTrainingSummaries();
    }

	public class TempFixDuplicatesStatisticsProvider : StatisticsProvider
	{
		public TempFixDuplicatesStatisticsProvider(IUserGeneratedDataRepositoryProviderFactory userGeneratedDataRepositoryProviderFactory, ITrainingSummaryRepository trainingSummaryRepository) : base(userGeneratedDataRepositoryProviderFactory, trainingSummaryRepository)
		{
		}

		public override async Task<IEnumerable<TrainingSummary?>> GetTrainingSummaries(IEnumerable<int> trainingIds)
		{
			var tmp = await base.GetTrainingSummaries(trainingIds);
            var aoa = tmp.GroupBy(o => o.Id)
                .ToDictionary(
                    o => o.Key,
                    o => o.OrderByDescending(o => o.TrainedDays).ThenBy(o => o.LastLogin)
                    );

            return tmp;
		}

		public override async Task<IEnumerable<TrainingDayAccount>> GetTrainingDays(int trainingId)
		{
			var tmp = await base.GetTrainingDays(trainingId);
            var aoa = tmp.GroupBy(o => $"{o.TrainingDay}_{o.StartTime}")
                .ToDictionary(
                o => o.Key,
                o => o.OrderByDescending(o => o.NumQuestions).ThenBy(o => o.EndTimeStamp).First()
                );
            return aoa.Values;
		}

		public override async Task<IEnumerable<PhaseStatistics>> GetPhaseStatistics(int trainingId)
		{
			var tmp = await base.GetPhaseStatistics(trainingId);
            var aoa = tmp.GroupBy(o => $"{o.training_day}_${o.exercise}_${o.timestamp}")
                .ToDictionary(
                o => o.Key,
                o => 
                {
                    var ordered = o
                        .OrderByDescending(o => o.end_timestamp)
                        .ThenByDescending(o => o.num_questions);
                    return ordered.First();
                });
            return aoa.Values;
		}
	}


	public class StatisticsProvider : IStatisticsProvider
    {
        private readonly IUserGeneratedDataRepositoryProviderFactory userGeneratedDataRepositoryProviderFactory;
        private readonly ITrainingSummaryRepository trainingSummaryRepository;

        //private readonly ITypedTableClientFactory typedTableClientFactory;

        public StatisticsProvider(IUserGeneratedDataRepositoryProviderFactory userGeneratedDataRepositoryProviderFactory, ITrainingSummaryRepository trainingSummaryRepository) //ITypedTableClientFactory typedTableClientFactory)
        {
            this.userGeneratedDataRepositoryProviderFactory = userGeneratedDataRepositoryProviderFactory;
            this.trainingSummaryRepository = trainingSummaryRepository;
            //this.typedTableClientFactory = typedTableClientFactory;
        }

        private IUserGeneratedDataRepositoryProvider GetDataProvider(int trainingId) =>
            userGeneratedDataRepositoryProviderFactory.Create(trainingId);

        public virtual async Task<IEnumerable<PhaseStatistics>> GetPhaseStatistics(int trainingId) =>
            (await GetDataProvider(trainingId).PhaseStatistics.GetAll()).OrderBy(o => o.training_day).ThenBy(o => o.timestamp).ToList();

        public virtual async Task<IEnumerable<TrainingDayAccount>> GetTrainingDays(int trainingId) =>
            (await GetDataProvider(trainingId).TrainingDays.GetAll()).OrderBy(o => o.TrainingDay).ToList();

        public virtual async Task<IEnumerable<TrainingSummary?>> GetTrainingSummaries(IEnumerable<int> trainingIds) =>
			await trainingSummaryRepository.GetByIds(trainingIds);

        public virtual Task<List<TrainingSummary>> GetAllTrainingSummaries() =>
            trainingSummaryRepository.GetAll();
    }
}
