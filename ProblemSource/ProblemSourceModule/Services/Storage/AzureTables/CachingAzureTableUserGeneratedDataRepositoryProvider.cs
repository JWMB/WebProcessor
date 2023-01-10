using Microsoft.Extensions.Caching.Memory;

namespace ProblemSource.Services.Storage.AzureTables
{
    public class CachingAzureTableUserGeneratedDataRepositoryProvider : AzureTableUserGeneratedDataRepositoryProvider
    {
        private readonly IMemoryCache cache;
        private readonly int userId;

        public CachingAzureTableUserGeneratedDataRepositoryProvider(IMemoryCache cache, ITypedTableClientFactory tableClientFactory, int userId)
            : base(tableClientFactory, userId)
        {
            this.cache = cache;
            this.userId = userId;
        }

        protected override IBatchRepository<T> Create<T>(IBatchRepository<T> inner, Func<T, string> createKey) =>
            new CachingBatchRepositoryFacade<T>(cache, inner, $"Training_{userId}_{typeof(T).Name}_", createKey);
    }
}
