using MongoDB.Driver;

namespace ProblemSourceModule.Services.Storage.MongoDb
{
    public class MongoTools
    {
  //      public static async Task Initialize()
  //      {
		//	const string uri = "mongodb://localhost:27017/";
		//	var client = new MongoClient(uri);
  //          var db = client.GetDatabase("training");
		//}

        public static IMongoCollection<T> GetCollection<T>(IMongoDatabase db) => db.GetCollection<T>(GetCollectionName(typeof(T)));

        public static async Task DropCollection<T>(IMongoDatabase db)
        {
            await db.DropCollectionAsync(GetCollectionName<T>());
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
}
