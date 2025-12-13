using MongoDB.Driver;
using ProblemSource.Models;
using ProblemSource.Models.Aggregates;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Models.Aggregates;

namespace ProblemSourceModule.Services.Storage.MongoDb
{
    public class MongoUserGeneratedDataRepositoryProvider : IUserGeneratedDataRepositoryProvider
    {
        private readonly IMongoDatabase db;
        private readonly int trainingId;
        private readonly string Key = $"{nameof(MongoDocumentWrapper<int>)}.{nameof(MongoDocumentWrapper<int>.RowKey)}";

        public MongoUserGeneratedDataRepositoryProvider(IMongoDatabase db, int trainingId)
        {
            this.db = db;
            this.trainingId = trainingId;
        }

        public IBatchRepository<Phase> Phases => new MongoBatchRepository<Phase, string>(db, Key, Phase.UniqueIdWithinUser, trainingId);
        public IBatchRepository<TrainingDayAccount> TrainingDays => new MongoBatchRepository<TrainingDayAccount, int>(db, Key, item => item.TrainingDay, trainingId);
		public IBatchRepository<PhaseStatistics> PhaseStatistics => new MongoBatchRepository<PhaseStatistics, string>(db, Key, ProblemSource.Models.Aggregates.PhaseStatistics.UniqueIdWithinUser, trainingId);
		public IBatchRepository<TrainingSummary> TrainingSummaries => new MongoBatchRepository<TrainingSummary, string>(db, Key, item => "x", trainingId);
		public IBatchRepository<UserGeneratedState> UserStates => new MongoBatchRepository<UserGeneratedState, string>(db, Key, item => "x", trainingId);

		public Task RemoveAll() => throw new NotImplementedException();
    }

	public class MongoBatchRepository<TDocument, TId> : IBatchRepository<TDocument>
	{
		private readonly int trainingId;
		private readonly string trainingIdField;
		private readonly DbCollection<MongoDocumentWrapper<TDocument>, TId> collection;

		public MongoBatchRepository(IMongoDatabase db, string idField, Func<TDocument, TId> getId, int trainingId, string trainingIdField = "trainingId")
		{
			collection = new DbCollection<MongoDocumentWrapper<TDocument>, TId>(db, $"{nameof(MongoDocumentWrapper<TDocument>.Document)}.{idField}", item => getId(item.Document));

			this.trainingId = trainingId;
			this.trainingIdField = trainingIdField;
		}

		private FilterDefinition<MongoDocumentWrapper<TDocument>> GetTrainingIdFilter() => DbCollection<MongoDocumentWrapper<TDocument>, int>.GetIdFilter(trainingId, $"{nameof(MongoDocumentWrapper<TDocument>.Document)}.{trainingIdField}");
		public async Task<IEnumerable<TDocument>> GetAll() => (await collection.ListAsync(GetTrainingIdFilter())).Select(o => o.Document).ToList();

		public async Task<int> RemoveAll() => await collection.RemoveAsync(GetTrainingIdFilter());

		public async Task<(IEnumerable<TDocument> Added, IEnumerable<TDocument> Updated)> Upsert(IEnumerable<TDocument> items)
		{
			Func<MongoDocumentWrapper<TDocument>, FilterDefinition<MongoDocumentWrapper<TDocument>>> createFilter = item =>
				Builders<MongoDocumentWrapper<TDocument>>.Filter.And(GetTrainingIdFilter(), collection.GetIdFilter(item));
			var (added, upserted) = await collection.Upsert(items.Select(o => new MongoDocumentWrapper<TDocument>(o)), createFilter);
			return (added.Select(o => o.Document), upserted.Select(o => o.Document));
		}
	}
}
