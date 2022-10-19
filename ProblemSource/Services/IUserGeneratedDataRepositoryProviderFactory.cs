using ProblemSource.Models.Aggregates;
using ProblemSource.Services.Storage;

namespace ProblemSource.Services
{
    public interface IUserGeneratedDataRepositoryProviderFactory
    {
        IUserGeneratedDataRepositoryProvider Create(string userId);
    }

    public class UserGeneratedDataRepositoriesProviderFactory : IUserGeneratedDataRepositoryProviderFactory
    {
        private readonly ITableClientFactory tableClientFactory;

        public UserGeneratedDataRepositoriesProviderFactory(ITableClientFactory tableClientFactory)
        {
            this.tableClientFactory = tableClientFactory;
        }
        public IUserGeneratedDataRepositoryProvider Create(string userId)
        {
            return new UserGeneratedDataRepositoryProvider(tableClientFactory, userId);
        }
    }
}
