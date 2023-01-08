using ProblemSource.Models.Aggregates;
using ProblemSourceModule.Models.Aggregates;

namespace ProblemSource.Services.Storage
{
    public interface IUserGeneratedDataRepositoryProvider
    {
        IBatchRepository<Phase> Phases { get; }
        IBatchRepository<TrainingDayAccount> TrainingDays { get; }
        IBatchRepository<PhaseStatistics> PhaseStatistics { get; }
        IBatchRepository<TrainingSummary> TrainingSummaries { get; }
    }
}
