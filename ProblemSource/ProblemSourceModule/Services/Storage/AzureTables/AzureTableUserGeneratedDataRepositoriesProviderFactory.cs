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
}
