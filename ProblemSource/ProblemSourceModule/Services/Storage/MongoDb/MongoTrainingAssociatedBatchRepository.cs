using MongoDB.Driver;
using ProblemSource.Services.Storage;

namespace ProblemSourceModule.Services.Storage.MongoDb
{
	public class MongoTrainingAssociatedDocumentWrapper<TDocument> : MongoDocumentWrapper<TDocument> // where TDocument : DocumentBase
	{
		// An additional property TrainingId for easy joining
        public MongoTrainingAssociatedDocumentWrapper() { }

		[System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
		public MongoTrainingAssociatedDocumentWrapper(TDocument doc, int trainingId, Func<TDocument, string>? getId = null) : base(doc) //getId
		{
			TrainingId = trainingId;
		}
		public int TrainingId { get; set; }
	}

	public class InnerIdConfig<TDocument, TId>
	{
		public required Func<TDocument, TId> Getter { get; set; }
		public required Action<TDocument> Setter { get; set; }
		public required string Field { get; set; }
	}

	public class MongoTrainingAssociatedBatchRepository<TDocument, TId> : IBatchRepository<TDocument>
	{
		private readonly int trainingId;
		private readonly DbTrainingAssociatedWrappedCollection<TDocument, TId> collection;

		public IMongoCollection<MongoTrainingAssociatedDocumentWrapper<TDocument>> GetCollection() => collection.GetCollection();

		//public MongoTrainingAssociatedBatchRepository(IMongoDatabase db, Func<TDocument, TId> getId, int trainingId, string compoundIdField)
		//{
		//	collection = new DbTrainingAssociatedWrappedCollection<TDocument, TId>(
		//		db, getId, item => new MongoTrainingAssociatedDocumentWrapper<TDocument>(item, trainingId, o => getId(o)?.ToString() ?? ""), compoundIdField);
  //          this.trainingId = trainingId;
		//}
		public MongoTrainingAssociatedBatchRepository(IMongoDatabase db, int trainingId, InnerIdConfig<TDocument, TId> innerIdConfig)
		{
			//collection = new DbTrainingAssociatedWrappedCollection<TDocument, TId>(
			//	db, innerIdConfig.Getter, item => new MongoTrainingAssociatedDocumentWrapper<TDocument>(item, trainingId, o => innerIdConfig.Getter(o)?.ToString() ?? ""), innerIdConfig.Field);
			collection = new DbTrainingAssociatedWrappedCollection<TDocument, TId>(
				db, innerIdConfig, item => new MongoTrainingAssociatedDocumentWrapper<TDocument>(item, trainingId, o => innerIdConfig.Getter(o)?.ToString() ?? ""));
			this.trainingId = trainingId;
		}

		private FilterDefinition<MongoTrainingAssociatedDocumentWrapper<TDocument>> GetTrainingIdFilter()
			=> DbCollectionWithId<MongoTrainingAssociatedDocumentWrapper<TDocument>, int>.GetIdFilter(trainingId, $"{nameof(MongoTrainingAssociatedDocumentWrapper<TDocument>.TrainingId)}");

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
