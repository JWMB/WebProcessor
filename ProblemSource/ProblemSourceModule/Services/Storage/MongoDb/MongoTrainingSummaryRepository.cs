using MongoDB.Driver;
using ProblemSourceModule.Models.Aggregates;

namespace ProblemSourceModule.Services.Storage.MongoDb
{
    public class MongoTrainingSummaryRepository : /* DbWrappedCollection<TrainingSummary, int>,*/ ITrainingSummaryRepository
	{
        private DbWrappedCollection<TrainingSummary, int> collection;

		public MongoTrainingSummaryRepository(IMongoDatabase db) //: base(db, item => 0, item => new MongoDocumentWrapper<TrainingSummary>(item, o => "0"))
        {
            collection = new DbWrappedCollection<TrainingSummary, int>(db, item => 0, item => new MongoDocumentWrapper<TrainingSummary>(item, o => "0"));
        }

        public async Task<List<TrainingSummary>> GetAll() => (await collection.GetAll()).ToList();
        //Task<List<TrainingSummary>> ITrainingSummaryRepository.GetAll() => base.GetAll();
    }
}
