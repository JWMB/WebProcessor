using MongoDB.Driver;
using ProblemSource.Services.Storage;

namespace ProblemSourceModule.Services.Storage.MongoDb
{
	public class MongoTrainingAssociatedDocumentWrapper<TDocument> : MongoDocumentWrapper<TDocument> // where TDocument : DocumentBase
	{
        public MongoTrainingAssociatedDocumentWrapper() { }

		[System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
		public MongoTrainingAssociatedDocumentWrapper(TDocument doc, int trainingId, Func<TDocument, string>? getId = null) : base(doc, getId)
        {
			TrainingId = trainingId;
		}
		public int TrainingId { get; set; }
	}

	public class MongoTrainingBatchRepository<TDocument, TId> : IBatchRepository<TDocument>
	{
        private readonly Func<TDocument, TId> getId;
        private readonly int trainingId;
		private readonly DbWrappedCollection<TDocument, TId> collection;

		public IMongoCollection<MongoDocumentWrapper<TDocument>> GetCollection() => collection.GetCollection();

		public MongoTrainingBatchRepository(IMongoDatabase db, Func<TDocument, TId> getId, int trainingId)
		{
			collection = new DbWrappedCollection<TDocument, TId>(db, getId, item => new MongoTrainingAssociatedDocumentWrapper<TDocument>(item, trainingId, o => getId(o)?.ToString() ?? ""));
            this.getId = getId;
            this.trainingId = trainingId;
		}

		private FilterDefinition<MongoDocumentWrapper<TDocument>> GetTrainingIdFilter()
			=> DbCollection<MongoDocumentWrapper<TDocument>, int>.GetIdFilter(trainingId, $"{nameof(MongoTrainingAssociatedDocumentWrapper<TDocument>.TrainingId)}");
		public async Task<IEnumerable<TDocument>> GetAll() => await collection.GetAll(GetTrainingIdFilter()); //(await collection.ListAsync(GetTrainingIdFilter())).Select(o => o.Document).ToList();
		public async Task<int> RemoveAll() => await collection.RemoveAll(GetTrainingIdFilter());

		public async Task<(IEnumerable<TDocument> Added, IEnumerable<TDocument> Updated)> Upsert(IEnumerable<TDocument> items)
		{
			//Func<MongoDocumentWrapper<TDocument>, FilterDefinition<MongoDocumentWrapper<TDocument>>> createFilter = item =>
			//	Builders<MongoDocumentWrapper<TDocument>>.Filter.And(
			//		GetTrainingIdFilter(),
			//		Builders<MongoDocumentWrapper<TDocument>>.Filter.Eq(o => o.RowKey, getId(item.Document)?.ToString()) //nameof(MongoDocumentWrapper<TDocument>.RowKey), o => getId(o.Document)?.ToString()
			//		);
			var (added, upserted) = await collection.Upsert(items, GetTrainingIdFilter()); //createFilter
			return (added, upserted);
		}
	}
}
