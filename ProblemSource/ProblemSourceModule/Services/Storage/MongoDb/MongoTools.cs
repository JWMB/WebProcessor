using MongoDB.Driver;

namespace ProblemSourceModule.Services.Storage.MongoDb
{
    public class MongoTools
    {
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
                    return type.GenericTypeArguments[0].Name;
				if (type.GetGenericTypeDefinition() == typeof(MongoTrainingAssociatedDocumentWrapper<>))
					return type.GenericTypeArguments[0].Name;
			}
			return type.Name;
        }

        public record MongoConfig(string ConnectionString, string Database);
	}
}
