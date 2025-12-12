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
		public static string GetCollectionName(Type type) => type.Name;
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
            //item.Id
            return getId(item);
        }

		public FilterDefinition<TDocument> GetIdFilter(TDocument id) => GetIdFilter(getId(id), idField); // Builders<TDocument>.Filter.Eq(idField, id);
		public FilterDefinition<TDocument> GetIdFilter(TId id) => GetIdFilter(id, idField); // Builders<TDocument>.Filter.Eq(idField, id);
		public FilterDefinition<TDocument> GetIdFilter(IEnumerable<TId> ids) => GetIdFilter(ids, idField); // Builders<TDocument>.Filter.AnyIn(idField, ids);

		//      public static FilterDefinition<TDocument> GetIdFilter<TId_>(TId_ id, string idField) => Builders<TDocument>.Filter.Eq(idField, id);
		//public static FilterDefinition<TDocument> GetIdFilter<TId_>(IEnumerable<TId_> ids, string idField) => Builders<TDocument>.Filter.AnyIn(idField, ids);
		public static FilterDefinition<TDocument> GetIdFilter(TId id, string idField) => Builders<TDocument>.Filter.Eq(idField, id);
		public static FilterDefinition<TDocument> GetIdFilter(IEnumerable<TId> ids, string idField) => Builders<TDocument>.Filter.AnyIn(idField, ids);

		public async Task<TDocument?> Get(TId id)
        {
            //var filter = Builders<TDocument>.Filter.Eq(idField, id);
            return await (await collection.FindAsync(GetIdFilter(id))).FirstOrDefaultAsync();
			//await collection.Find(o => o.Email == id).FirstOrDefaultAsync();
        }

		public async Task<IEnumerable<TDocument>> GetAll() => await collection.Find(o => true).ToListAsync();

		public async Task Remove(TDocument item)
		{
			var found = await collection.FindOneAndDeleteAsync(GetIdFilter(getId(item)));
		}

		public async Task Update(TDocument item) => await collection.FindOneAndReplaceAsync(GetIdFilter(getId(item)), item);
        public async Task Upsert(TDocument item) => await Update(item);

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
		//protected async Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(FilterDefinition<TDocument> filter, FindOptions<TDocument, TProjection>? options = null, CancellationToken cancellationToken = default(CancellationToken))
		//{
		//    if (options == null) throw new ArgumentNullException(nameof(options));
		//    return await collection.FindAsync<TDocument>(filter, options, cancellationToken);
		//}

	}

	public class MongoUserGeneratedDataRepositoryProvider : IUserGeneratedDataRepositoryProvider
    {
        private readonly IMongoDatabase db;
        private readonly int trainingId;

        public MongoUserGeneratedDataRepositoryProvider(IMongoDatabase db, int trainingId)
        {
            this.db = db;
            this.trainingId = trainingId;
        }

        public IBatchRepository<Phase> Phases => new MongoBatchRepository<Phase, string>(db, "", Phase.UniqueIdWithinUser, trainingId);

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
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

		public required TDocument Document { get; set; }
    }

    public class MongoBatchRepository<TDocument, TId> : IBatchRepository<TDocument>
    {
        private readonly int trainingId;
        private readonly string trainingIdField;
		//private readonly Func<TDocument, TId> getId;
		private readonly DbCollection<TDocument, TId> collection;
		//private readonly DbCollection<MongoDocumentWrapper<TDocument>, TId> collection;

		public MongoBatchRepository(IMongoDatabase db, string idField, Func<TDocument, TId> getId, int trainingId, string trainingIdField = "trainingId")
        {
			collection = new DbCollection<TDocument, TId>(db, idField, getId);
			//collection = new DbCollection<MongoDocumentWrapper<TDocument>, TId>(db, $"{nameof(MongoDocumentWrapper<TDocument>.Document)}.{idField}", item => getId(item.Document));

			this.trainingId = trainingId;
            this.trainingIdField = trainingIdField;
            //this.getId = getId;
        }

		//private FilterDefinition<MongoDocumentWrapper<TDocument>> GetTrainingIdFilter() => DbCollection<MongoDocumentWrapper<TDocument>, int>.GetIdFilter(trainingId, $"{nameof(MongoDocumentWrapper<TDocument>.Document)}.{trainingIdField}");
		private FilterDefinition<TDocument> GetTrainingIdFilter() => DbCollection<TDocument, int>.GetIdFilter(trainingId, trainingIdField);
		//public async Task<IEnumerable<TDocument>> GetAll() => (await collection.ListAsync(GetTrainingIdFilter())).Select(o => o.Document).ToList();
		public async Task<IEnumerable<TDocument>> GetAll() => await collection.ListAsync(GetTrainingIdFilter());

		public async Task<int> RemoveAll()
            => await collection.RemoveAsync(GetTrainingIdFilter());

        public async Task<(IEnumerable<TDocument> Added, IEnumerable<TDocument> Updated)> Upsert(IEnumerable<TDocument> items)
        {
            Func<TDocument, FilterDefinition<TDocument>> createFilter = item =>
                Builders<TDocument>.Filter.And(GetTrainingIdFilter(), collection.GetIdFilter(item));
            return await collection.Upsert(items, createFilter);
        }
	}


    public class MongoUserRepository : DbCollection<User, string>, IUserRepository
    {
        public MongoUserRepository(IMongoDatabase db) : base(db, nameof(User.Email), u => u.Email)
		{ }
        public async Task Add(User item) => await InsertGetId(item);
        //public async Task<User?> Get(string id) => await collection.Find(o => o.Email == id).FirstOrDefaultAsync();
	}

    public class MongoTrainingRepository : DbCollection<Training, int>, ITrainingRepository
    {
		public MongoTrainingRepository(IMongoDatabase db) : base(db, nameof(Training.Id), u => u.Id)
		{ }

        public Task Add(Training item) => AddGetId(item);

        public async Task<int> AddGetId(Training item) => await InsertGetId(item);

        public async Task<IEnumerable<Training>> GetByIds(IEnumerable<int> ids)
        {
            var list = ids.ToList();
			return await (await collection.FindAsync(o => list.Contains(o.Id))).ToListAsync();
        }
		//public Task Remove(Training item) => throw new NotImplementedException();
		//public Task Update(Training item) => throw new NotImplementedException();
		//public Task Upsert(Training item) => throw new NotImplementedException();
		//public async Task<Training?> Get(int id) => await base.Get(id);
		//public Task<IEnumerable<Training>> GetAll() => throw new NotImplementedException();
	}

}
