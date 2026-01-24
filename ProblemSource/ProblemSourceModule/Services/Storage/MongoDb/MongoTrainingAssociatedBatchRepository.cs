using MongoDB.Driver;
using ProblemSource.Services.Storage;

namespace ProblemSourceModule.Services.Storage.MongoDb
{
	public class MongoTrainingAssociatedDocumentWrapper<TDocument> : MongoDocumentWrapper<TDocument> // where TDocument : DocumentBase
	{
		// An additional property TrainingId for easy joining
        public MongoTrainingAssociatedDocumentWrapper() { }

		[System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
		public MongoTrainingAssociatedDocumentWrapper(TDocument doc, int trainingId, Func<TDocument, string>? getId = null) : base(doc, getId)
        {
			TrainingId = trainingId;
		}
		public int TrainingId { get; set; }
	}

	public class MongoTrainingAssociatedBatchRepository<TDocument, TId> : IBatchRepository<TDocument>
	{
		//private readonly Func<TDocument, TId> getId;
		private readonly int trainingId;
		private readonly DbTrainingAssociatedWrappedCollection<TDocument, TId> collection;
		//private readonly DbWrappedCollection<TDocument, TId> collection;

		public IMongoCollection<MongoTrainingAssociatedDocumentWrapper<TDocument>> GetCollection() => collection.GetCollection();

		public MongoTrainingAssociatedBatchRepository(IMongoDatabase db, Func<TDocument, TId> getId, int trainingId)
		{
			collection = new DbTrainingAssociatedWrappedCollection<TDocument, TId>(db, getId, item => new MongoTrainingAssociatedDocumentWrapper<TDocument>(item, trainingId, o => getId(o)?.ToString() ?? ""));
            //this.getId = getId;
            this.trainingId = trainingId;
		}

		private FilterDefinition<MongoTrainingAssociatedDocumentWrapper<TDocument>> GetTrainingIdFilter()
			=> DbCollectionWithId<MongoTrainingAssociatedDocumentWrapper<TDocument>, int>.GetIdFilter(trainingId, $"{nameof(MongoTrainingAssociatedDocumentWrapper<TDocument>.TrainingId)}");
		//private FilterDefinition<MongoTrainingAssociatedDocumentWrapper<TDocument>> GetTrainingIdFilter()
		//{
		//	return DbCollection<MongoTrainingAssociatedDocumentWrapper<TDocument>, int>.GetIdFilter(trainingId, $"{nameof(MongoTrainingAssociatedDocumentWrapper<TDocument>.TrainingId)}");
		//}

		public async Task<IEnumerable<TDocument>> GetAll() //=> await collection.GetAll(GetTrainingIdFilter())
		{
			var filter = GetTrainingIdFilter();

			var serializerRegistry = MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry;
			var documentSerializer = serializerRegistry.GetSerializer<MongoTrainingAssociatedDocumentWrapper<TDocument>>();
			var tmp = filter.Render(new RenderArgs<MongoTrainingAssociatedDocumentWrapper<TDocument>>(documentSerializer, serializerRegistry));
			return await collection.GetAll(filter);
		}
			 //(await collection.ListAsync(GetTrainingIdFilter())).Select(o => o.Document).ToList();

		public async Task<int> RemoveAll()
			=> await collection.RemoveAll(GetTrainingIdFilter());

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
