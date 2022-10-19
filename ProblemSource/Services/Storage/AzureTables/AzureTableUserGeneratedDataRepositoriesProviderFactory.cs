namespace ProblemSource.Services.Storage.AzureTables
{
    public class AzureTableUserGeneratedDataRepositoriesProviderFactory : IUserGeneratedDataRepositoryProviderFactory
    {
        private readonly ITableClientFactory tableClientFactory;

        public AzureTableUserGeneratedDataRepositoriesProviderFactory(ITableClientFactory tableClientFactory)
        {
            this.tableClientFactory = tableClientFactory;
        }
        public IUserGeneratedDataRepositoryProvider Create(string userId)
        {
            return new AzureTableUserGeneratedDataRepositoryProvider(tableClientFactory, userId);
        }
    }
}
