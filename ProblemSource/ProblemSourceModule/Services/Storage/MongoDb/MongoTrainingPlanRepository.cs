using MongoDB.Driver;
using ProblemSourceModule.Models;

namespace ProblemSourceModule.Services.Storage.MongoDb
{
    public class MongoUserRepository : DbWrappedCollection<User, string>, IUserRepository
    {
        public MongoUserRepository(IMongoDatabase db) : base(db, nameof(User.Email), u => u.Document.Email)
		{ }
        public async Task Add(User item) => await Upsert(item);
	}

	public class MongoTrainingRepository : DbWrappedCollection<Training, int>, ITrainingRepository
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
			await Upsert(item);

			semaphore.Release();
            return item.Id;
        }

        public async Task<IEnumerable<Training>> GetByIds(IEnumerable<int> ids) => await Get(ids);
	}
}
