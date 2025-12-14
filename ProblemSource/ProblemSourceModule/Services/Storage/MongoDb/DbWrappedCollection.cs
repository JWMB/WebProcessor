using MongoDB.Driver;

namespace ProblemSourceModule.Services.Storage.MongoDb
{
    public class DbWrappedCollection<TDocument, TId>
    {
        protected DbCollection<MongoDocumentWrapper<TDocument>, TId> collection;
        private readonly Func<TDocument, TId> getId;
        private readonly Func<TDocument, MongoDocumentWrapper<TDocument>> createWrapped;

        //private readonly Func<MongoDocumentWrapper<TDocument>, TId> getId;


        //public DbWrappedCollection(IMongoDatabase db, string idField, Func<MongoDocumentWrapper<TDocument>, TId> getId)
        //public DbWrappedCollection(IMongoDatabase db, string idField, Func<TDocument, TId> getId)
        public DbWrappedCollection(IMongoDatabase db, Func<TDocument, TId> getId, Func<TDocument, MongoDocumentWrapper<TDocument>> createWrapped)
		{
			collection = new DbCollection<MongoDocumentWrapper<TDocument>, TId>(db, nameof(MongoDocumentWrapper<TDocument>.RowKey), wrapper => getId(wrapper.Document));
            this.getId = getId;
            this.createWrapped = createWrapped;
        }

		public IMongoCollection<MongoDocumentWrapper<TDocument>> GetCollection() => collection.GetCollection();

		private MongoDocumentWrapper<TDocument> CreateWrapped(TDocument item) => createWrapped(item); // new MongoDocumentWrapper<TDocument>(item, o => getId(o)?.ToString() ?? "");

		public Task Remove(TDocument item) => collection.Remove(CreateWrapped(item));
		//public async Task<int> RemoveAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
		//{
		//	var result = await collection.RemoveAsync(filter, cancellationToken);
		//	return (int)result.DeletedCount;
		//}
		//public async Task<List<TDocument>> ListAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
		//{
		//	return (await collection.ListAsync(filter)).Select(o => o.Document).ToList();
		//	//return await (await collection.FindAsync<TDocument>(filter, null, cancellationToken)).ToListAsync(cancellationToken);
		//}

		//public string GetIdFilter(MongoDocumentWrapper<TDocument> item)
		//{
		//	return "AAA";
		//}

		//public async Task<(IEnumerable<TDocument> Added, IEnumerable<TDocument> Updated)> Upsert(IEnumerable<TDocument> items, Func<TDocument, FilterDefinition<TDocument>> createFilter)
		//{
		//	return ([], []);
		//}
		public async Task<(IEnumerable<TDocument> Added, IEnumerable<TDocument> Updated)> Upsert(IEnumerable<TDocument> items, FilterDefinition<MongoDocumentWrapper<TDocument>>? globalFilter = null) //, Func<MongoDocumentWrapper<TDocument>, FilterDefinition<MongoDocumentWrapper<TDocument>>> createFilter)
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
		public async Task<IEnumerable<TDocument>> GetAll(FilterDefinition<MongoDocumentWrapper<TDocument>> filter)
			=> (await collection.ListAsync(filter)).Select(o => o.Document).ToList();
		public async Task<int> RemoveAll(FilterDefinition<MongoDocumentWrapper<TDocument>> filter)
			=> await collection.RemoveAsync(filter);

	}
}
