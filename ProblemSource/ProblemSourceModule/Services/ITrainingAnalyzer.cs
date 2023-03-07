using ProblemSource.Models;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Models;
using ProblemSource.Models.LogItems;

namespace ProblemSourceModule.Services
{

    public interface ITrainingAnalyzer
    {
        Task<bool> Analyze(Training training, IUserGeneratedDataRepositoryProvider provider, List<LogItem>? latestLogItems);

        static async Task<int?> WasDayJustCompleted(Training training, IUserGeneratedDataRepositoryProvider provider, List<LogItem>? latestLogItems)
        {
            // Hmm - seems client filters out EndOfDayLogItem before syncing
            if (latestLogItems?.LastOrDefault() is EndOfDayLogItem eod)
                return eod.training_day;

            var latestDaySummary = (await provider.TrainingDays.GetAll()).OrderBy(o => o.TrainingDay).LastOrDefault();
            if (latestDaySummary == null)
                return null;

            if (training.Settings.timeLimits.Any())
            {
                var totalMinutes = latestDaySummary.RemainingMinutes + latestDaySummary.ResponseMinutes;
                if (totalMinutes > training.Settings.timeLimits.First() * 0.9m)
                    return latestDaySummary.TrainingDay;
            }

            var timeSinceSync = DateTimeOffset.UtcNow.DateTime - latestDaySummary.EndTimeStamp;  // TODO: use some updatedAt / LastLogin value (EndTimeStamp is client's local timestamp)
            //var timeSinceSync = DateTimeOffset.UtcNow - latestDaySummary.EndTimeStamp;
            if (timeSinceSync < TimeSpan.FromHours(1)) // Need to wait until they've probably finished training for the day
                return null;

            return latestDaySummary.TrainingDay;
        }
    }
}
