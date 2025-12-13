using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ProblemSourceModule.Services.Storage.MongoDb
{
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
}
