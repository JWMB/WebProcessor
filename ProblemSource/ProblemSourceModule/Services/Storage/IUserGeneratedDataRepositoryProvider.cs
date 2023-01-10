using ProblemSource.Models;
using ProblemSource.Models.Aggregates;
using ProblemSourceModule.Models.Aggregates;
using ProblemSourceModule.Services.Storage;

namespace ProblemSource.Services.Storage
{
    public interface IUserGeneratedDataRepositoryProvider
    {
        IBatchRepository<Phase> Phases { get; }
        IBatchRepository<TrainingDayAccount> TrainingDays { get; }
        IBatchRepository<PhaseStatistics> PhaseStatistics { get; }

        IBatchRepository<TrainingSummary> TrainingSummaries { get; }
        //IRepository<UserGeneratedState, string> UserStates { get; }
        IBatchRepository<UserGeneratedState> UserStates { get; }
    }
}
