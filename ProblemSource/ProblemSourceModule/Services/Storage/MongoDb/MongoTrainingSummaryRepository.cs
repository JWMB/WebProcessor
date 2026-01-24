using MongoDB.Driver;
using ProblemSourceModule.Models.Aggregates;

namespace ProblemSourceModule.Services.Storage.MongoDb
{
    public interface IMongoRepoWithCollection<TDocument, TId>
    {
		DbWrappedCollection<TDocument, TId> Collection { get; }
	}
    public class MongoTrainingSummaryRepository : /* DbWrappedCollection<TrainingSummary, int>,*/ ITrainingSummaryRepository, IMongoRepoWithCollection<TrainingSummary, int>
	{
        private DbWrappedCollection<TrainingSummary, int> collection;

        public DbWrappedCollection<TrainingSummary, int> Collection => collection;

        public MongoTrainingSummaryRepository(IMongoDatabase db) //: base(db, item => 0, item => new MongoDocumentWrapper<TrainingSummary>(item, o => "0"))
        {
            collection = new DbWrappedCollection<TrainingSummary, int>(db, item => 0, item => new MongoDocumentWrapper<TrainingSummary>(item, o => "0"));
        }

        public async Task<List<TrainingSummary>> GetAll() => (await collection.GetAll()).ToList();
    }
}
