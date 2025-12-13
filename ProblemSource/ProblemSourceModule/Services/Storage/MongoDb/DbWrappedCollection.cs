using MongoDB.Driver;

namespace ProblemSourceModule.Services.Storage.MongoDb
{
    public class DbWrappedCollection<TDocument, TId>
    {
        protected DbCollection<MongoDocumentWrapper<TDocument>, TId> collection;
		public DbWrappedCollection(IMongoDatabase db, string idField, Func<MongoDocumentWrapper<TDocument>, TId> getId)
        {
            collection = new DbCollection<MongoDocumentWrapper<TDocument>, TId>(db, idField, getId);
		}

        public Task Remove(TDocument item) => collection.Remove(new MongoDocumentWrapper<TDocument>(item));
        public Task Update(TDocument item) => collection.Update(new MongoDocumentWrapper<TDocument>(item));
		public Task Upsert(TDocument item) => collection.Upsert(new MongoDocumentWrapper<TDocument>(item));
		public async Task<TDocument?> Get(TId id)
        {
            var found = await collection.Get(id);
            if (found != null)
                return found.Document;
            return default;
        }
        public async Task<List<TDocument>> Get(IEnumerable<TId> ids) => (await collection.Get(ids)).Select(o => o.Document).ToList();

		public async Task<IEnumerable<TDocument>> GetAll() => (await collection.GetAll()).Select(o => o.Document).ToList();
	}
}
