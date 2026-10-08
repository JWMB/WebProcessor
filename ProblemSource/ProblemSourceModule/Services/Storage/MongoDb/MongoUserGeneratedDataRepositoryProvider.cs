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
			//=> new MongoTrainingAssociatedBatchRepository<Phase, string>(db, Phase.UniqueIdWithinUser, trainingId, ""); //Key, 
			=> new MongoTrainingAssociatedBatchRepository<Phase, string>(db, trainingId, 
				new InnerIdConfig<Phase, string>
				{
					Getter = Phase.UniqueIdWithinUser,
					Setter = Phase.SetUniqueIdWithinUser,
					Field = $"Document.{nameof(Phase.CompoundId)}"
				});

		public IBatchRepository<TrainingDayAccount> TrainingDays
			//=> new MongoTrainingAssociatedBatchRepository<TrainingDayAccount, int>(db, item => item.TrainingDay, trainingId, ""); // "AccountId", 
			=> new MongoTrainingAssociatedBatchRepository<TrainingDayAccount, int>(db, trainingId,
				new InnerIdConfig<TrainingDayAccount, int>
				{
					Getter = tda => tda.TrainingDay,
					Setter = TrainingDayAccount.SetUniqueIdWithinUser,
					Field = $"Document.{nameof(TrainingDayAccount.CompoundId)}"
				});

		public IBatchRepository<PhaseStatistics> PhaseStatistics
			//=> new MongoTrainingAssociatedBatchRepository<PhaseStatistics, string>(db, ProblemSource.Models.Aggregates.PhaseStatistics.UniqueIdWithinUser, trainingId,
			//	nameof(ProblemSource.Models.Aggregates.PhaseStatistics.CompoundId)); // "account_id", 
			=> new MongoTrainingAssociatedBatchRepository<PhaseStatistics, string>(db, trainingId,
				new InnerIdConfig<PhaseStatistics, string>
				{
					Getter = ProblemSource.Models.Aggregates.PhaseStatistics.UniqueIdWithinUser,
					Setter = ProblemSource.Models.Aggregates.PhaseStatistics.SetUniqueIdWithinUser,
					Field = $"Document.{nameof(ProblemSource.Models.Aggregates.PhaseStatistics.CompoundId)}"
				});

		public IBatchRepository<TrainingSummary> TrainingSummaries
			//=> new MongoTrainingAssociatedBatchRepository<TrainingSummary, string>(db, item => "x", trainingId, ""); // "AccountId",
			=> new MongoTrainingAssociatedBatchRepository<TrainingSummary, string>(db, trainingId,
				new InnerIdConfig<TrainingSummary, string>
				{
					Getter = ts => "",
					Setter = ts => { },
					Field = ""
				});


		public IBatchRepository<UserGeneratedState> UserStates
			//=> new MongoTrainingAssociatedBatchRepository<UserGeneratedState, string>(db, item => "x", trainingId, ""); // Key, 
			=> new MongoTrainingAssociatedBatchRepository<UserGeneratedState, string>(db, trainingId,
				new InnerIdConfig<UserGeneratedState, string>
				{
					Getter = ts => "",
					Setter = ts => { },
					Field = ""
				});


		public async Task RemoveAll()
		{
			await Phases.RemoveAll();
			await TrainingDays.RemoveAll();
			await PhaseStatistics.RemoveAll();
			await TrainingSummaries.RemoveAll();
			await UserStates.RemoveAll();
		}
	}
}
