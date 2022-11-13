using ProblemSource.Models.Aggregates;

namespace ProblemSource.Services.Storage
{
    public interface IUserGeneratedDataRepositoryProvider
    {
        IBatchRepository<Phase> Phases { get; }
        IBatchRepository<TrainingDayAccount> TrainingDays { get; }
        IBatchRepository<PhaseStatistics> PhaseStatistics { get; }
    }
}
