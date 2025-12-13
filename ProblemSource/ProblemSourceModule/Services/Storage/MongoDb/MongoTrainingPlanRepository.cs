using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using ProblemSource.Models;
using ProblemSource.Models.Aggregates;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Models;
using ProblemSourceModule.Models.Aggregates;

namespace ProblemSourceModule.Services.Storage.MongoDb
{

    public class MongoTools
    {
        public static async Task Initialize()
        {
			const string uri = "mongodb://localhost:27017/";
			var client = new MongoClient(uri);
            var db = client.GetDatabase("training");
            //db.GetCollection<>
		}

        public static string GetCollectionName<T>() => GetCollectionName(typeof(T));
		public static string GetCollectionName(Type type)
        {
            if (type.GenericTypeArguments.Any())
            {
                if (type.GetGenericTypeDefinition() == typeof(MongoDocumentWrapper<>))
                {
                    return type.GenericTypeArguments[0].Name;
				}
            }
            return type.Name;
        }
	}

    public class DbWrappedCollection<TDocument, TId>
    {
        protected DbCollection<MongoDocumentWrapper<TDocument>, TId> collection;
		public DbWrappedCollection(IMongoDatabase db, string idField, Func<MongoDocumentWrapper<TDocument>, TId> getId)// : base(db, idField, getId)
        {
            collection = new DbCollection<MongoDocumentWrapper<TDocument>, TId>(db, idField, getId);
		}

        public Task Remove(TDocument item) => collection.Remove(new MongoDocumentWrapper<TDocument>(item));
        public Task Update(TDocument item) => collection.Update(new MongoDocumentWrapper<TDocument>(item)); // TODO: is mongo ID handled properly?
		public Task Upsert(TDocument item) => collection.Upsert(new MongoDocumentWrapper<TDocument>(item)); // TODO: is mongo ID handled properly?
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

	public class MongoUserGeneratedDataRepositoryProvider : IUserGeneratedDataRepositoryProvider
    {
        private readonly IMongoDatabase db;
        private readonly int trainingId;
        private readonly string Key = $"{nameof(MongoDocumentWrapper<int>)}.{nameof(MongoDocumentWrapper<int>.RowKey)}";

        public MongoUserGeneratedDataRepositoryProvider(IMongoDatabase db, int trainingId)
        {
            this.db = db;
            this.trainingId = trainingId;
        }

        public IBatchRepository<Phase> Phases => new MongoBatchRepository<Phase, string>(db, Key, Phase.UniqueIdWithinUser, trainingId);

        public IBatchRepository<TrainingDayAccount> TrainingDays => throw new NotImplementedException();

        public IBatchRepository<PhaseStatistics> PhaseStatistics => throw new NotImplementedException();

        public IBatchRepository<TrainingSummary> TrainingSummaries => throw new NotImplementedException();

        public IBatchRepository<UserGeneratedState> UserStates => throw new NotImplementedException();

        public Task RemoveAll()
        {
            throw new NotImplementedException();
        }
    }

    public class MongoDocumentWrapper<TDocument>
    {
        public MongoDocumentWrapper()
        {
			Id = ObjectId.GenerateNewId();
		}

		[System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
		public MongoDocumentWrapper(TDocument doc) : this()
        {
            Document = doc;
        }

        [BsonId]
		public ObjectId Id { get; set; }

		//[BsonRepresentation(BsonType.ObjectId)]
		//[BsonId(IdGenerator = typeof(MongoDB.Bson.Serialization.IdGenerators.BsonObjectIdGenerator))]
		//[BsonIgnoreIfDefault]
		//public string Id { get; set; } = string.Empty;

		public string RowKey { get; set; } = string.Empty;

		public required TDocument Document { get; set; }
	}

    public class MongoBatchRepository<TDocument, TId> : IBatchRepository<TDocument>
    {
        private readonly int trainingId;
        private readonly string trainingIdField;
		//private readonly Func<TDocument, TId> getId;
		//private readonly DbCollection<TDocument, TId> collection;
		private readonly DbCollection<MongoDocumentWrapper<TDocument>, TId> collection;

		public MongoBatchRepository(IMongoDatabase db, string idField, Func<TDocument, TId> getId, int trainingId, string trainingIdField = "trainingId")
        {
			//collection = new DbCollection<TDocument, TId>(db, idField, getId);
			collection = new DbCollection<MongoDocumentWrapper<TDocument>, TId>(db, $"{nameof(MongoDocumentWrapper<TDocument>.Document)}.{idField}", item => getId(item.Document));

			this.trainingId = trainingId;
            this.trainingIdField = trainingIdField;
            //this.getId = getId;
        }

        private FilterDefinition<MongoDocumentWrapper<TDocument>> GetTrainingIdFilter() => DbCollection<MongoDocumentWrapper<TDocument>, int>.GetIdFilter(trainingId, $"{nameof(MongoDocumentWrapper<TDocument>.Document)}.{trainingIdField}");
        public async Task<IEnumerable<TDocument>> GetAll() => (await collection.ListAsync(GetTrainingIdFilter())).Select(o => o.Document).ToList();
        //private FilterDefinition<TDocument> GetTrainingIdFilter() => DbCollection<TDocument, int>.GetIdFilter(trainingId, trainingIdField);
        //public async Task<IEnumerable<TDocument>> GetAll() => await collection.ListAsync(GetTrainingIdFilter());

        public async Task<int> RemoveAll()
            => await collection.RemoveAsync(GetTrainingIdFilter());

        public async Task<(IEnumerable<TDocument> Added, IEnumerable<TDocument> Updated)> Upsert(IEnumerable<TDocument> items)
        {
            Func<MongoDocumentWrapper<TDocument>, FilterDefinition<MongoDocumentWrapper<TDocument>>> createFilter = item =>
                Builders<MongoDocumentWrapper<TDocument>>.Filter.And(GetTrainingIdFilter(), collection.GetIdFilter(item));
            var (added, upserted) = await collection.Upsert(items.Select(o => new MongoDocumentWrapper<TDocument>(o)), createFilter);
            return (added.Select(o => o.Document), upserted.Select(o => o.Document));
        }
	}


    public class MongoUserRepository : DbCollection<User, string>, IUserRepository
    {
        public MongoUserRepository(IMongoDatabase db) : base(db, nameof(User.Email), u => u.Email)
		{ }
        public async Task Add(User item) => await InsertGetId(item);
        //public async Task<User?> Get(string id) => await collection.Find(o => o.Email == id).FirstOrDefaultAsync();
	}

	public class MongoTrainingRepository : DbWrappedCollection<Training, int>, ITrainingRepository

	//public class MongoTrainingRepository : DbCollection<Training, int>, ITrainingRepository
    {
		private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

		public MongoTrainingRepository(IMongoDatabase db) : base(db, $"{nameof(MongoDocumentWrapper<int>.Document)}.{nameof(Training.Id)}", u => u.Document.Id)
		{ }

        public Task Add(Training item) => AddGetId(item);

        public async Task<int> AddGetId(Training item)
        {
			await semaphore.WaitAsync();

			var filter = Builders<MongoDocumentWrapper<Training>>.Filter.Empty;
			var count = await collection.CountDocumentsAsync();
            item.Id = (int)count + 1;
            //await collection.Upsert(new MongoDocumentWrapper<Training>(item));
			await Upsert(item);
			//await InsertGetId(item);

			semaphore.Release();
            return item.Id;
        }

        public async Task<IEnumerable<Training>> GetByIds(IEnumerable<int> ids)
        {
            var list = ids.ToList();
            return await Get(ids);
			//return await (await collection.FindAsync(o => list.Contains(o.Id))).ToListAsync();
		}
		//public Task Remove(Training item) => throw new NotImplementedException();
		//public Task Update(Training item) => throw new NotImplementedException();
		//public Task Upsert(Training item) => throw new NotImplementedException();
		//public async Task<Training?> Get(int id) => await base.Get(id);
		//public Task<IEnumerable<Training>> GetAll() => throw new NotImplementedException();
	}
}
