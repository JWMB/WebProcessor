using MongoDB.Driver;
using ProblemSourceModule.Models;

namespace ProblemSourceModule.Services.Storage.MongoDb
{
    public class MongoUserRepository : DbWrappedCollection<User, string>, IUserRepository
    {
        public MongoUserRepository(IMongoDatabase db) : base(db, u => u.Email, item => new MongoDocumentWrapper<User>(item, o => o.Email)) //nameof(User.Email), u => u.Document.Email
		{ }
        public async Task Add(User item) => await Upsert(item);
	}

	public class MongoTrainingRepository : DbWrappedCollection<Training, string>, ITrainingRepository
    {
		private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

		public MongoTrainingRepository(IMongoDatabase db) : base(db, u => u.Id.ToString(), item => new MongoDocumentWrapper<Training>(item, o => o.Id.ToString()))
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

        public Task<Training?> Get(int id) => Get(id.ToString());

        public async Task<IEnumerable<Training>> GetByIds(IEnumerable<int> ids) => await Get(ids.Select(o => o.ToString()));
	}
}
