using MongoDB.Bson;

namespace ProblemSourceModule.Services.Storage.MongoDb
{
    public class MongoDocumentWrapper<TDocument> : DocumentBase
	{
        public MongoDocumentWrapper()
        {
			//Id = ObjectId.GenerateNewId();
		}

		[System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
		public MongoDocumentWrapper(TDocument doc) : this() //Func<TDocument, string>? getId = null
		{
            Document = doc;
			//if (getId != null)
			//	RowKey = getId(doc);
        }

		//[BsonS]
		//public string RowKey { get; set; } = string.Empty;

		public required TDocument Document { get; set; }
	}
}
