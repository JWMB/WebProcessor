using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ProblemSourceModule.Services.Storage.MongoDb
{
    public class DocumentBase
    {
		//[BsonRepresentation(BsonType.ObjectId)]
		//[BsonId(IdGenerator = typeof(MongoDB.Bson.Serialization.IdGenerators.BsonObjectIdGenerator))]
		//[BsonIgnoreIfDefault]
		//public string Id { get; set; } = string.Empty;

		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public ObjectId Id { get; set; }
	}
}
