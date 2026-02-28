using ProblemSourceModule.Models.Aggregates;

namespace ProblemSourceModule.Services.Storage
{
    public interface ITrainingSummaryRepository
    {
        Task<List<TrainingSummary>> GetAll();
		Task<List<TrainingSummary>> GetByIds(IEnumerable<int> trainingIds);
	}
}
