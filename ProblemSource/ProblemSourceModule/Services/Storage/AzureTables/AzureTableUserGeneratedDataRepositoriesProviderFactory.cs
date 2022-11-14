namespace ProblemSource.Services.Storage.AzureTables
{
    public class AzureTableUserGeneratedDataRepositoriesProviderFactory : IUserGeneratedDataRepositoryProviderFactory
    {
        private readonly ITypedTableClientFactory tableClientFactory;

        public AzureTableUserGeneratedDataRepositoriesProviderFactory(ITypedTableClientFactory tableClientFactory)
        {
            this.tableClientFactory = tableClientFactory;
        }
        public IUserGeneratedDataRepositoryProvider Create(string userId)
        {
            return new AzureTableUserGeneratedDataRepositoryProvider(tableClientFactory, userId);
        }
    }
}
