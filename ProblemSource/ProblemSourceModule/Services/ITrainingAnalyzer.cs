using ProblemSource.Models.Aggregates;
using ProblemSource.Models;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Models;
using ProblemSource.Models.LogItems;
using Microsoft.Extensions.Logging;

namespace ProblemSourceModule.Services
{
    public class TrainingAnalyzerCollection
    {
        private readonly IEnumerable<ITrainingAnalyzer> instances;
        private readonly ILogger<TrainingAnalyzerCollection> log;

        public TrainingAnalyzerCollection(IEnumerable<ITrainingAnalyzer> instances, ILogger<TrainingAnalyzerCollection> log)
        {
            this.instances = instances;
            this.log = log;
        }

        public async Task Execute(Training training, List<LogItem> latestLogItems, IUserGeneratedDataRepositoryProvider provider)
        {
            if (instances?.Any() != true)
                return;

            foreach (var item in instances)
            {
                try
                {
                    await item.Analyze(training, latestLogItems, provider);
                }
                catch (Exception ex)
                {
                    log.LogWarning($"{nameof(MLFeaturesJulia)}: {ex.Message}", ex);
                }
            }
        }
    }

    public interface ITrainingAnalyzer
    {
        Task Analyze(Training training, List<LogItem> latestLogItems, IUserGeneratedDataRepositoryProvider provider);
    }

    public class XX : ITrainingAnalyzer
    {
        public async Task Analyze(Training training, List<LogItem> latestLogItems, IUserGeneratedDataRepositoryProvider provider)
        {
            var eod = latestLogItems.OfType<EndOfDayLogItem>().FirstOrDefault();
            // TODO: also check if stat's TrainingDay has changed

            if (eod?.training_day == 5)
            {
                var mlFeatures = MLFeaturesJulia.FromPhases(training.Settings ?? new TrainingSettings(), await provider.Phases.GetAll());
            }
        }
    }
}
