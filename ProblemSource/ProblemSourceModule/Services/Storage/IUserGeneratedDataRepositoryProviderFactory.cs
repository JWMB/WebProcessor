namespace ProblemSource.Services.Storage
{
    public interface IUserGeneratedDataRepositoryProviderFactory
    {
        IUserGeneratedDataRepositoryProvider Create(int userId);
    }
}
