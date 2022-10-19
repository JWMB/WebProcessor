using ProblemSource.Models.Aggregates;

namespace ProblemSource.Services.Storage
{
    public interface IUserGeneratedDataRepositoryProvider
    {
        IRepository<Phase> Phases { get; }
        IRepository<TrainingDayAccount> TrainingDays { get; }
        IRepository<PhaseStatistics> PhaseStatistics { get; }
    }
}
