using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace ProblemSourceModule.Services.Storage.MongoDb
{
    public class DbCollectionWithId<TDocument, TId> where TDocument : DocumentBase
    {
        protected IMongoCollection<TDocument> collection;
        private readonly string idField;
        private readonly Func<TDocument, TId> getId;

        public DbCollectionWithId(IMongoDatabase db, string idField, Func<TDocument, TId> getId)
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
		public FilterDefinition<TDocument> GetIdFilter(TId id) => GetIdFilter(id, idField); // Builders<TDocument>.Filter.Eq(idField, id);
		public FilterDefinition<TDocument> GetIdFilter(IEnumerable<TId> ids) => GetIdFilter(ids, idField); // Builders<TDocument>.Filter.AnyIn(idField, ids);

		public static FilterDefinition<TDocument> GetIdFilter(TId id, string idField) => Builders<TDocument>.Filter.Eq(idField, id);
		public static FilterDefinition<TDocument> GetIdFilter(IEnumerable<TId> ids, string idField) => Builders<TDocument>.Filter.AnyIn(idField, ids);

		public async Task<TDocument?> Get(TId id) => await (await collection.FindAsync(GetIdFilter(id))).FirstOrDefaultAsync();
		public async Task<List<TDocument>> Get(IEnumerable<TId> ids) //=> await (await collection.FindAsync(GetIdFilter(ids))).ToListAsync();
		{
			var filter = GetIdFilter(ids);
			return await (await collection.FindAsync(filter)).ToListAsync();
		}
		public async Task<IEnumerable<TDocument>> GetAll() => await collection.Find(o => true).ToListAsync();

		public async Task Remove(TDocument item)
		{
			var found = await collection.FindOneAndDeleteAsync(GetIdFilter(getId(item)));
		}

		public async Task Update(TDocument item) => await collection.FindOneAndReplaceAsync(GetIdFilter(getId(item)), item);
		public Task Upsert(TDocument item) => Upsert([item]);

		public async Task<List<TDocument>> ListAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
		{
			var cursor = await collection.FindAsync<TDocument>(filter, null, cancellationToken);
			return await cursor.ToListAsync(cancellationToken);
		}
		public async Task<int> RemoveAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
		{
            var result = await collection.DeleteManyAsync(filter, cancellationToken);
            return (int)result.DeletedCount;
		}

		private FilterDefinition<TDocument> GetFilter(IEnumerable<TDocument> items, FilterDefinition<TDocument>? globalFilter = null)
		{
			var filter = Builders<TDocument>.Filter.In(idField, items.Select(getId));
			if (globalFilter != null)
				filter = Builders<TDocument>.Filter.And(globalFilter, filter);
			return filter;
		}

		// Func<TDocument, FilterDefinition<TDocument>> createFilter, 
		public async Task<(IEnumerable<TDocument> Added, IEnumerable<TDocument> Updated)> Upsert(IEnumerable<TDocument> items, FilterDefinition<TDocument>? globalFilter = null)
        {
			var itemsWithId = items.Select(o => new { Id = getId(o), Item = o }).ToList();

			var projection = Builders<TDocument>.Projection.Include(idField); //Include("Id").
			var tmpX = (await collection.Find(GetFilter(items, globalFilter)).Project(projection).ToListAsync())
				.Select(o => new { Id = o["_id"].AsObjectId, SubId = o[idField]?.ToString() })
				//.Select(o => BsonSerializer.Deserialize<X>(o))
				.Where(o => o.SubId != null).ToList();
			if (tmpX == null)
				throw new Exception("A");

			var toInsert = itemsWithId.Where(o => tmpX.Any(p => p.SubId?.Equals(o.Id) == true) == false).ToList();
			var toReplace = itemsWithId.Where(o => tmpX.Any(p => p.SubId?.Equals(o.Id) == true) == true).ToList();

			var models = toInsert.Select(o => (WriteModel<TDocument>)new InsertOneModel<TDocument>(o.Item)).ToList();

			models.AddRange(toReplace.Select(o =>
			{
				var found = tmpX.Single(p => p.SubId?.Equals(o.Id) == true);
				if (found == null)
					throw new Exception("aaa");
				o.Item.Id = found.Id;
				return new ReplaceOneModel<TDocument>(Builders<TDocument>.Filter.Eq(p => p.Id, found.Id), o.Item); //createFilter(o.Item)
			}));

			if (models.Any() == false)
				return ([], []);
			var results = await collection.BulkWriteAsync(models, new BulkWriteOptions { });
			// hm, we can't tell which were inserted and which were replaced?!
			return (toInsert.Select(o => o.Item), toReplace.Select(o => o.Item));
		}

		public async Task<long> CountDocumentsAsync(FilterDefinition<TDocument>? filter = null)
        {
			return await collection.CountDocumentsAsync(filter ?? Builders<TDocument>.Filter.Empty, filter == null ? new CountOptions { Hint = "_id_" } : null);
		}
	}


	public class XObjectCustomSerializer : SerializerBase<object?>
	{
		public override object? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
		{
			//var name = context.Reader.ReadName(MongoDB.Bson.IO.Utf8NameDecoder.Instance);

			if (context.Reader.State == MongoDB.Bson.IO.BsonReaderState.Value)
			{
				//context.Reader.ReadNull();
				var rr = context.Reader.GetCurrentBsonType();
				if (rr == BsonType.Null)
				{
					context.Reader.ReadNull();
					return null;
				}
			}
			var ntype = args.NominalType;
			if (context.Reader.CurrentBsonType == BsonType.Null)
			{
				context.Reader.ReadNull();
				return null;
			}
			var json = context.Reader.ReadString();
			//if (json.StartsWith("["))
			try
			{
				var parsed = Newtonsoft.Json.Linq.JToken.Parse(json);
				//var value = System.Text.Json.JsonSerializer.Deserialize<Newtonsoft.Json.Linq.JObject>(json);
				return parsed;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"XObjectCustomSerializer {ex}");
				return null;
			}
		}

		public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object? value)
		{
			var serializerOptions = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
			if (value == null)
			{
				context.Writer.WriteNull();
				return;
			}

			var json = System.Text.Json.JsonSerializer.Serialize(value, serializerOptions);
			context.Writer.WriteString(json);
		}
	}
	public class JObjectCustomSerializer : SerializerBase<Newtonsoft.Json.Linq.JObject>
	{
		public override Newtonsoft.Json.Linq.JObject Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
		{
			var json = context.Reader.ReadString();
			var value = System.Text.Json.JsonSerializer.Deserialize<Newtonsoft.Json.Linq.JObject>(json);
			return value!;
		}

		public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Newtonsoft.Json.Linq.JObject value)
		{
			var serializerOptions = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
			var json = System.Text.Json.JsonSerializer.Serialize(value, serializerOptions);
			context.Writer.WriteString(json);
		}
	}
}
