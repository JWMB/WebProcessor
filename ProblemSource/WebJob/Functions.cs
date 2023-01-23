using Microsoft.Azure.WebJobs;
using ProblemSourceModule.Models;
using ProblemSourceModule.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebJob
{
    public class Functions
    {
        [FunctionName(nameof(Functions.MyContinuousMethod))]
        [NoAutomaticTrigger]
        public async Task MyContinuousMethod()
        {
            //var works = new RunDayAnalyzers()
            while (true)
            {
                await Task.Delay(10000);
            }
        }
    }

    public abstract class Work
    {
        public abstract bool ShouldRun();
        public abstract Task Run();

        protected bool IsNightTime => DateTime.Now.Hour > 22 || DateTime.Now.Hour < 6;

        public DateTimeOffset LastRun { get; set; }
        protected TimeSpan MinInterval { get; set; } = TimeSpan.FromMinutes(10);
        protected bool MinIntervalHasPassed => (DateTimeOffset.Now - LastRun) > MinInterval;
    }

    public class RunDayAnalyzers : Work
    {
        private readonly TrainingAnalyzerCollection trainingAnalyzers;

        public RunDayAnalyzers(TrainingAnalyzerCollection trainingAnalyzers)
        {
            MinInterval = TimeSpan.FromHours(3);
            this.trainingAnalyzers = trainingAnalyzers;
        }

        public override async Task Run()
        {
            // TODO: Find trainings that synced during the day
            var trainings = new List<Training>();

            foreach (var training in trainings)
            {
                // TODO: check if training has already been checked
                var trainingHasBeenAnalyzed = false;
                if (trainingHasBeenAnalyzed == false)
                {
                    var modified = await trainingAnalyzers.Execute(training, null, null);
                    if (modified)
                    {
                        // 
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
