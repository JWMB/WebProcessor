using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace ProblemSourceModule.Services.Storage.MongoDb
{
    public class DbCollection<TDocument, TId> where TDocument : DocumentBase
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

		public IMongoCollection<TDocument> GetCollection() => collection;
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
			var itemsWithId = items.Select(o => new { Id = getId(o), Item = o }).ToList();

			var filter = Builders<TDocument>.Filter.In(idField, itemsWithId.Select(o => o.Id));
			//var projection = new FindExpressionProjectionDefinition<TDocument, X>(p => new X { Id = p.Id, SubId = getId(p) });
			//ProjectionDefinition<BsonDocument> projection = $$"""{ "Id": "Id", "SubId": "{{idField}}" }""";
			var tmpAll = await collection.Find(o => true).ToListAsync();
			var projection = Builders<TDocument>.Projection.Include(idField); //Include("Id").
			var tmpX = (await collection.Find(filter).Project(projection).ToListAsync())
				.Select(o => new { Id = o["_id"].AsObjectId, SubId = o[idField]?.ToString() })
				//.Select(o => BsonSerializer.Deserialize<X>(o))
				.Where(o => o.SubId != null).ToList();
			if (tmpX == null)
				throw new Exception("A");
			//var bsonIdById = tmpX.ToDictionary(o => o.SubId!, o => o.Id);

			var toInsert = itemsWithId.Where(o => tmpX.Any(p => p.SubId?.Equals(o.Id) == true) == false).ToList();
			var toReplace = itemsWithId.Where(o => tmpX.Any(p => p.SubId?.Equals(o.Id) == true) == true).ToList();
			//var toInsert = itemsWithId.Select(o => bsonIdById.GetValueOrDefault(o.Id!.ToString())).ToList();
			//var toReplace = itemsWithId.Where(o => tmpX.Any(p => p.SubId?.Equals(o.Id) == true) == true).ToList();

			var models = toInsert.Select(o => (WriteModel<TDocument>)new InsertOneModel<TDocument>(o.Item)).ToList();

			models.AddRange(toReplace.Select(o =>
			{
				var found = tmpX.Single(p => p.SubId?.Equals(o.Id) == true);
				if (found == null)
					throw new Exception("aaa");
				o.Item.Id = found.Id;
				return new ReplaceOneModel<TDocument>(createFilter(o.Item), o.Item);
			}));

			var results = await collection.BulkWriteAsync(models, new BulkWriteOptions { });
			// hm, we can't tell which were inserted and which were replaced?!
			return (toInsert.Select(o => o.Item), toReplace.Select(o => o.Item));
		}

		private class X
		{
			public ObjectId Id { get; set; }
			//public object? SubId { get; set; }
			public TId? SubId { get; set; }
		}

		public async Task<long> CountDocumentsAsync(FilterDefinition<TDocument>? filter = null)
        {
			return await collection.CountDocumentsAsync(filter ?? Builders<TDocument>.Filter.Empty, filter == null ? new CountOptions { Hint = "_id_" } : null);
		}
	}
}
