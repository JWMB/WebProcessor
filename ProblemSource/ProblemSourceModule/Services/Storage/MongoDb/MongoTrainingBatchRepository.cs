using MongoDB.Driver;

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

	public class MongoTrainingBatchRepository<TDocument, TId> //: IBatchRepository<TDocument>
	{
        private readonly Func<TDocument, TId> getId;
        private readonly int trainingId;
		//private readonly string trainingIdField;
		//private readonly DbCollection<MongoDocumentWrapper<TDocument>, TId> collection;
		private readonly DbWrappedCollection<TDocument, TId> collection;
		//private readonly DbWrappedCollection<TDocument, TId> collection;

		public IMongoCollection<MongoDocumentWrapper<TDocument>> GetCollection() => collection.GetCollection();

		//public MongoTrainingBatchRepository(IMongoDatabase db, string idField, Func<TDocument, TId> getId, int trainingId, string trainingIdField = "trainingId")
		public MongoTrainingBatchRepository(IMongoDatabase db, Func<TDocument, TId> getId, int trainingId) //, string trainingIdField = "trainingId")
		{
			//collection = new DbCollection<MongoDocumentWrapper<TDocument>, TId>(db, $"{nameof(MongoDocumentWrapper<TDocument>.Document)}.{idField}", item => getId(item.Document));
			//collection = new DbCollection<MongoDocumentWrapper<TDocument>, TId>(db, $"{idField}", item => getId(item.Document));
			collection = new DbWrappedCollection<TDocument, TId>(db, getId, item => new MongoTrainingAssociatedDocumentWrapper<TDocument>(item, trainingId, o => getId(o)?.ToString() ?? ""));
            this.getId = getId;
            this.trainingId = trainingId;
			//this.trainingIdField = trainingIdField;
		}

		private FilterDefinition<MongoDocumentWrapper<TDocument>> GetTrainingIdFilter()
			=> DbCollection<MongoDocumentWrapper<TDocument>, int>.GetIdFilter(trainingId, $"{nameof(MongoTrainingAssociatedDocumentWrapper<TDocument>.TrainingId)}");
		//private FilterDefinition<MongoDocumentWrapper<TDocument>> GetTrainingIdFilter()
		//	=> DbCollection<MongoDocumentWrapper<TDocument>, int>.GetIdFilter(trainingId, $"{nameof(MongoDocumentWrapper<TDocument>.Document)}.{trainingIdField}");
		//private FilterDefinition<MongoTrainingAssociatedDocumentWrapper<TDocument>> GetTrainingIdFilter()
		//	=> DbCollection<MongoTrainingAssociatedDocumentWrapper<TDocument>, int>.GetIdFilter(trainingId, nameof(MongoTrainingAssociatedDocumentWrapper<TDocument>.TrainingId)); //$"{nameof(MongoDocumentWrapper<TDocument>.Document)}.{trainingIdField}");

		//public async Task<IEnumerable<TDocument>> GetAll() => (await collection.ListAsync(GetTrainingIdFilter())).Select(o => o.Document).ToList();

		//public async Task<int> RemoveAll() => await collection.RemoveAsync(GetTrainingIdFilter());

		public async Task<(IEnumerable<TDocument> Added, IEnumerable<TDocument> Updated)> Upsert(IEnumerable<TDocument> items)
		{
			Func<MongoDocumentWrapper<TDocument>, FilterDefinition<MongoDocumentWrapper<TDocument>>> createFilter = item =>
				Builders<MongoDocumentWrapper<TDocument>>.Filter.And(
					GetTrainingIdFilter(),
					Builders<MongoDocumentWrapper<TDocument>>.Filter.Eq(o => o.RowKey, getId(item.Document)?.ToString()) //nameof(MongoDocumentWrapper<TDocument>.RowKey), o => getId(o.Document)?.ToString()
					);
			//var wrapped = items.Select(o => new MongoDocumentWrapper<TDocument>(o)).ToList();
			//foreach (var item in wrapped)
			//	item.RowKey = getId(wrapped);
			var (added, upserted) = await collection.Upsert(items, createFilter);
			//return (added.Select(o => o.Document), upserted.Select(o => o.Document));
			return (added, upserted);
		}
	}
}
