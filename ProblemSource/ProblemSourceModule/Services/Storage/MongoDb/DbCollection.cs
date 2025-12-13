using MongoDB.Driver;

namespace ProblemSourceModule.Services.Storage.MongoDb
{
    public class DbCollection<TDocument, TId>
    {
        protected IMongoCollection<TDocument> collection;
        private readonly string idField;
        private readonly Func<TDocument, TId> getId;

        public DbCollection(IMongoDatabase db, string idField, Func<TDocument, TId> getId)
        {
			collection = db.GetCollection<TDocument>(MongoTools.GetCollectionName<TDocument>());
            this.idField = idField;
            this.getId = getId;
        }

		public async Task<TId> InsertGetId(TDocument item)
		{
			await collection.InsertOneAsync(item);
			return getId(item);
        }

		public FilterDefinition<TDocument> GetIdFilter(TDocument id) => GetIdFilter(getId(id), idField); // Builders<TDocument>.Filter.Eq(idField, id);
		public FilterDefinition<TDocument> GetIdFilter(TId id) => GetIdFilter(id, idField); // Builders<TDocument>.Filter.Eq(idField, id);
		public FilterDefinition<TDocument> GetIdFilter(IEnumerable<TId> ids) => GetIdFilter(ids, idField); // Builders<TDocument>.Filter.AnyIn(idField, ids);

		//      public static FilterDefinition<TDocument> GetIdFilter<TId_>(TId_ id, string idField) => Builders<TDocument>.Filter.Eq(idField, id);
		//public static FilterDefinition<TDocument> GetIdFilter<TId_>(IEnumerable<TId_> ids, string idField) => Builders<TDocument>.Filter.AnyIn(idField, ids);
		public static FilterDefinition<TDocument> GetIdFilter(TId id, string idField) => Builders<TDocument>.Filter.Eq(idField, id);
		public static FilterDefinition<TDocument> GetIdFilter(IEnumerable<TId> ids, string idField) => Builders<TDocument>.Filter.AnyIn(idField, ids);

		public async Task<TDocument?> Get(TId id) => await (await collection.FindAsync(GetIdFilter(id))).FirstOrDefaultAsync();
		public async Task<List<TDocument>> Get(IEnumerable<TId> ids) => await (await collection.FindAsync(GetIdFilter(ids))).ToListAsync();

		public async Task<IEnumerable<TDocument>> GetAll() => await collection.Find(o => true).ToListAsync();

		public async Task Remove(TDocument item)
		{
			var found = await collection.FindOneAndDeleteAsync(GetIdFilter(getId(item)));
		}

		public async Task Update(TDocument item) => await collection.FindOneAndReplaceAsync(GetIdFilter(getId(item)), item);
        public async Task Upsert(TDocument item) => await collection.FindOneAndReplaceAsync(GetIdFilter(getId(item)), item, new FindOneAndReplaceOptions<TDocument, TDocument> { IsUpsert = true });

		public async Task<List<TDocument>> ListAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
		{
			return await (await collection.FindAsync<TDocument>(filter, null, cancellationToken)).ToListAsync(cancellationToken);
		}
		public async Task<int> RemoveAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
		{
            var result = await collection.DeleteManyAsync(filter, cancellationToken);
            return (int)result.DeletedCount;
		}

		public async Task<(IEnumerable<TDocument> Added, IEnumerable<TDocument> Updated)> Upsert(IEnumerable<TDocument> items, Func<TDocument, FilterDefinition<TDocument>> createFilter)
        {
            var list = items.ToList();
            var models = items.Select(o => new ReplaceOneModel<TDocument>(createFilter(o), o) { IsUpsert = true });
            //InsertOneModel
			var results = await collection.BulkWriteAsync(models, new BulkWriteOptions { });
            var upsertedIndices = results.Upserts.Select(o => o.Index).ToList();

            var upserted = upsertedIndices.Select(o => list[o]);

			return (list.Except(upserted), upserted);
		}

        public async Task<long> CountDocumentsAsync(FilterDefinition<TDocument>? filter = null)
        {
			return await collection.CountDocumentsAsync(filter ?? Builders<TDocument>.Filter.Empty, filter == null ? new CountOptions { Hint = "_id_" } : null);
		}
	}
}
