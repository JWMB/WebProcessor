using Microsoft.Extensions.Caching.Memory;

namespace ProblemSource.Services.Storage.AzureTables
{
    public class AzureTableUserGeneratedDataRepositoriesProviderFactory : IUserGeneratedDataRepositoryProviderFactory
    {
        private readonly ITypedTableClientFactory tableClientFactory;

        public AzureTableUserGeneratedDataRepositoriesProviderFactory(ITypedTableClientFactory tableClientFactory)
        {
            this.tableClientFactory = tableClientFactory;
        }
        public IUserGeneratedDataRepositoryProvider Create(int userId)
        {
            return new AzureTableUserGeneratedDataRepositoryProvider(tableClientFactory, userId);
        }
    }

    public class CachingAzureTableUserGeneratedDataRepositoriesProviderFactory : IUserGeneratedDataRepositoryProviderFactory
    {
        private readonly IMemoryCache cache;
        private readonly ITypedTableClientFactory tableClientFactory;

        public CachingAzureTableUserGeneratedDataRepositoriesProviderFactory(IMemoryCache cache, ITypedTableClientFactory tableClientFactory)
        {
            this.cache = cache;
            this.tableClientFactory = tableClientFactory;
        }
        public IUserGeneratedDataRepositoryProvider Create(int userId)
        {
            return new CachingAzureTableUserGeneratedDataRepositoryProvider(cache, tableClientFactory, userId);
        }
    }

}
