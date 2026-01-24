using MongoDB.Driver;

namespace ProblemSourceModule.Services.Storage.MongoDb
{
	//public class DbWrappedCollectionOfT<T, TDocument, TId> 
	//	where T : DbWrappedCollection<TDocument, TId>
	//	where TDocument : MongoDocumentWrapper<TDocument>

	public class DbTrainingAssociatedWrappedCollection<TDocument, TId>
	{
		protected DbCollectionWithId<MongoTrainingAssociatedDocumentWrapper<TDocument>, TId> collection;
		private readonly Func<TDocument, TId> getId;
		private readonly Func<TDocument, MongoTrainingAssociatedDocumentWrapper<TDocument>> createWrapped;

		public DbTrainingAssociatedWrappedCollection(IMongoDatabase db, Func<TDocument, TId> getId, Func<TDocument, MongoTrainingAssociatedDocumentWrapper<TDocument>> createWrapped)
		{
			collection = new DbCollectionWithId<MongoTrainingAssociatedDocumentWrapper<TDocument>, TId>(db, nameof(MongoTrainingAssociatedDocumentWrapper<TDocument>.RowKey), wrapper => getId(wrapper.Document));
			this.getId = getId;
			this.createWrapped = createWrapped;
		}

		public IMongoCollection<MongoTrainingAssociatedDocumentWrapper<TDocument>> GetCollection() => collection.GetCollection();

		private MongoTrainingAssociatedDocumentWrapper<TDocument> CreateWrapped(TDocument item) => createWrapped(item); // new MongoDocumentWrapper<TDocument>(item, o => getId(o)?.ToString() ?? "");

		public Task Remove(TDocument item) => collection.Remove(CreateWrapped(item));

		public async Task<(IEnumerable<TDocument> Added, IEnumerable<TDocument> Updated)> Upsert(IEnumerable<TDocument> items, FilterDefinition<MongoTrainingAssociatedDocumentWrapper<TDocument>>? globalFilter = null) //, Func<MongoDocumentWrapper<TDocument>, FilterDefinition<MongoDocumentWrapper<TDocument>>> createFilter)
		{
			var result = await collection.Upsert(items.Select(CreateWrapped), globalFilter); //createFilter
			return (result.Added.Select(o => o.Document), result.Updated.Select(o => o.Document));
		}

		public Task Update(TDocument item) => Upsert([item]); // TODO: throw if not already existing collection.Update(CreateWrapped(item));
		public Task Upsert(TDocument item) => Upsert([item]); // collection.Upsert(CreateWrapped(item));
		public async Task<TDocument?> Get(TId id)
		{
			var found = await collection.Get(id);
			if (found != null)
				return found.Document;
			return default;
		}
		public async Task<List<TDocument>> Get(IEnumerable<TId> ids) => (await collection.Get(ids)).Select(o => o.Document).ToList();
		public async Task<IEnumerable<TDocument>> GetAll() => (await collection.GetAll()).Select(o => o.Document).ToList();
		public async Task<IEnumerable<TDocument>> GetAll(FilterDefinition<MongoTrainingAssociatedDocumentWrapper<TDocument>> filter)
			=> (await collection.ListAsync(filter)).Select(o => o.Document).ToList();
		public async Task<int> RemoveAll(FilterDefinition<MongoTrainingAssociatedDocumentWrapper<TDocument>> filter)
			=> await collection.RemoveAsync(filter);
	}

	public class DbWrappedCollection<TDocument, TId>
    {
        protected DbCollectionWithId<MongoDocumentWrapper<TDocument>, TId> collection;
        private readonly Func<TDocument, MongoDocumentWrapper<TDocument>> createWrapped;

        public DbWrappedCollection(IMongoDatabase db, Func<TDocument, TId> getId, Func<TDocument, MongoDocumentWrapper<TDocument>> createWrapped)
		{
			collection = new DbCollectionWithId<MongoDocumentWrapper<TDocument>, TId>(db, nameof(MongoDocumentWrapper<TDocument>.RowKey), wrapper => getId(wrapper.Document));
            this.createWrapped = createWrapped;
        }

		public IMongoCollection<MongoDocumentWrapper<TDocument>> GetCollection() => collection.GetCollection();

		private MongoDocumentWrapper<TDocument> CreateWrapped(TDocument item) => createWrapped(item);

		public Task Remove(TDocument item) => collection.Remove(CreateWrapped(item));

		public async Task<(IEnumerable<TDocument> Added, IEnumerable<TDocument> Updated)> Upsert(IEnumerable<TDocument> items, FilterDefinition<MongoDocumentWrapper<TDocument>>? globalFilter = null) //, Func<MongoDocumentWrapper<TDocument>, FilterDefinition<MongoDocumentWrapper<TDocument>>> createFilter)
		{
			var result = await collection.Upsert(items.Select(CreateWrapped), globalFilter); //createFilter
			return (result.Added.Select(o => o.Document), result.Updated.Select(o => o.Document));
		}

		public Task Update(TDocument item) => Upsert([item]); // TODO: throw if not already existing collection.Update(CreateWrapped(item));
		public Task Upsert(TDocument item) => Upsert([item]);
		public async Task<TDocument?> Get(TId id)
        {
            var found = await collection.Get(id);
            if (found != null)
                return found.Document;
            return default;
        }
        public async Task<List<TDocument>> Get(IEnumerable<TId> ids) => (await collection.Get(ids)).Select(o => o.Document).ToList();

		public async Task<IEnumerable<TDocument>> GetAll() => (await collection.GetAll()).Select(o => o.Document).ToList();
		public async Task<IEnumerable<TDocument>> GetAll(FilterDefinition<MongoDocumentWrapper<TDocument>> filter)
			=> (await collection.ListAsync(filter)).Select(o => o.Document).ToList();
		public async Task<int> RemoveAll(FilterDefinition<MongoDocumentWrapper<TDocument>> filter)
			=> await collection.RemoveAsync(filter);
	}
}
