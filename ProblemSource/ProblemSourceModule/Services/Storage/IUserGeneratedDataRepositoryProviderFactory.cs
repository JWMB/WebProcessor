using ProblemSource.Models.Aggregates;

namespace ProblemSource.Services.Storage
{
    public interface IUserGeneratedDataRepositoryProviderFactory
    {
        IUserGeneratedDataRepositoryProvider Create(string userId);
    }
}
