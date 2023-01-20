using ProblemSource.Models.Aggregates;
using ProblemSource.Models;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Models;
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

        public async Task<bool> Execute(Training training, List<LogItem> latestLogItems, IUserGeneratedDataRepositoryProvider provider)
        {
            if (instances?.Any() != true)
                return false;

            var modified = false;
            foreach (var item in instances)
            {
                try
                {
                    modified |= await item.Analyze(training, latestLogItems, provider);
                }
                catch (Exception ex)
                {
                    log.LogWarning($"{nameof(MLFeaturesJulia)}: {ex.Message}", ex);
                    return false;
                }
            }
            return modified;
        }
    }
}
