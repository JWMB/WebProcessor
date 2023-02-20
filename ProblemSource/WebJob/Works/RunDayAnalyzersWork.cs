using Microsoft.Extensions.Logging;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Services;
using ProblemSourceModule.Services.Storage;

namespace WebJob.Works
{
    public class RunDayAnalyzersWork : WorkBase
    {
        private readonly TrainingAnalyzerCollection trainingAnalyzers;
        private readonly ITrainingRepository trainingRepository;
        private readonly IUserGeneratedDataRepositoryProviderFactory dataRepositoryProviderFactory;
        private readonly ILogger<RunDayAnalyzersWork> log;

        public RunDayAnalyzersWork(TrainingAnalyzerCollection trainingAnalyzers, ITrainingRepository trainingRepository,
            IUserGeneratedDataRepositoryProviderFactory dataRepositoryProviderFactory, ILogger<RunDayAnalyzersWork> log)
        {
            MinInterval = TimeSpan.FromHours(3);
            this.trainingAnalyzers = trainingAnalyzers;
            this.trainingRepository = trainingRepository;
            this.dataRepositoryProviderFactory = dataRepositoryProviderFactory;
            this.log = log;
        }

        public override async Task Run()
        {
            // TODO: Find trainings that synced during the day
            // The *TrainingSummaries repo filtered by timestamp should be the quickest
            var trainingIds = new List<int>();

            foreach (var trainingId in trainingIds)
            {
                // TODO: check if training has already been checked
                var trainingHasBeenAnalyzed = false;
                if (trainingHasBeenAnalyzed == false)
                {
                    var training = await trainingRepository.Get(trainingId);
                    if (training == null)
                        continue;

                    var modified = await trainingAnalyzers.Execute(training, dataRepositoryProviderFactory.Create(training.Id), null);
                    if (modified)
                    {
                        log.LogInformation($"Training {training.Id} was modified");
                        await trainingRepository.Update(training);
                    }
                }
            }
        }

        public override bool ShouldRun()
        {
            return IsNightTime && MinIntervalHasPassed;
        }
    }
}
