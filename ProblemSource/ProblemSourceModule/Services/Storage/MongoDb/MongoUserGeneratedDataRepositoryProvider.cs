using MongoDB.Driver;
using ProblemSource.Models;
using ProblemSource.Models.Aggregates;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Models.Aggregates;

namespace ProblemSourceModule.Services.Storage.MongoDb
{
    public class MongoUserGeneratedDataRepositoryProviderFactory : IUserGeneratedDataRepositoryProviderFactory
    {
        private readonly IMongoDatabase db;

        public MongoUserGeneratedDataRepositoryProviderFactory(IMongoDatabase db)
        {
            this.db = db;
        }
        public IUserGeneratedDataRepositoryProvider Create(int userId)
        {
			return new MongoUserGeneratedDataRepositoryProvider(db, userId);
        }
    }

    public class MongoUserGeneratedDataRepositoryProvider : IUserGeneratedDataRepositoryProvider
	{
		private readonly IMongoDatabase db;
		private readonly int trainingId;

		public MongoUserGeneratedDataRepositoryProvider(IMongoDatabase db, int trainingId)
		{
			this.db = db;
			this.trainingId = trainingId;
		}

		public IBatchRepository<Phase> Phases
			=> new MongoTrainingBatchRepository<Phase, string>(db, Phase.UniqueIdWithinUser, trainingId); //Key, 

		public IBatchRepository<TrainingDayAccount> TrainingDays
			=> new MongoTrainingBatchRepository<TrainingDayAccount, int>(db, item => item.TrainingDay, trainingId); // "AccountId", 

		public IBatchRepository<PhaseStatistics> PhaseStatistics
			=> new MongoTrainingBatchRepository<PhaseStatistics, string>(db, ProblemSource.Models.Aggregates.PhaseStatistics.UniqueIdWithinUser, trainingId); // "account_id", 

		public IBatchRepository<TrainingSummary> TrainingSummaries
			=> new MongoTrainingBatchRepository<TrainingSummary, string>(db, item => "x", trainingId); // "AccountId", 

		public IBatchRepository<UserGeneratedState> UserStates
			=> new MongoTrainingBatchRepository<UserGeneratedState, string>(db, item => "x", trainingId); // Key, 

		public Task RemoveAll() => throw new NotImplementedException();
	}
}
