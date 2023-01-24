using ProblemSource.Models.Aggregates;
using ProblemSource.Models;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Models;
using ProblemSource.Models.LogItems;

namespace ProblemSourceModule.Services
{

    public interface ITrainingAnalyzer
    {
        Task<bool> Analyze(Training training, IUserGeneratedDataRepositoryProvider provider, List<LogItem>? latestLogItems);

        static async Task<bool> WasDayJustCompleted(int day, IUserGeneratedDataRepositoryProvider provider, List<LogItem>? latestLogItems)
        {
            var eod = latestLogItems?.OfType<EndOfDayLogItem>().FirstOrDefault();
            if (eod != null)
                return eod.training_day == day;

            var summary = (await provider.TrainingSummaries.GetAll()).FirstOrDefault();
            if (summary == null || summary.TrainedDays != day)
                return false;

            var timeSinceSync = DateTimeOffset.UtcNow - summary.LastLogin;
            return timeSinceSync >= TimeSpan.FromHours(1); // Need to wait until they've probably finished training for the day
        }
    }
}
