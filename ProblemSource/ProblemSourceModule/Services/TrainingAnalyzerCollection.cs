using ProblemSource.Models;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Models;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

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

        public async Task<bool> Execute(Training training, IUserGeneratedDataRepositoryProvider provider, List<LogItem>? latestLogItems)
        {
            if (instances?.Any() != true)
                return false;

            if (training.Settings?.Analyzers?.Any() != true)
                return false;

            var modified = false;
            foreach (var item in instances)
            {
                if (training.Settings.Analyzers.Any(o => Regex.IsMatch(item.GetType().Name, o)) == false)
                    continue;

                try
                {
                    modified |= await item.Analyze(training, provider, latestLogItems);
                }
                catch (Exception ex)
                {
                    log.LogWarning($"{item.GetType().Name}: {ex.Message}", ex);
                    return false;
                }
            }
            return modified;
        }
    }
}
