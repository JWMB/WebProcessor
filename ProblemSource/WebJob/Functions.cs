using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace WebJob
{
    public class Functions
    {
        private readonly IWorkInstanceProvider workInstanceProvider;
        private readonly ILogger<Functions> log;

        public Functions(IWorkInstanceProvider workInstanceProvider, ILogger<Functions> log)
        {
            this.workInstanceProvider = workInstanceProvider;
            this.log = log;
        }

        [Singleton]
        [FunctionName(nameof(Functions.ContinuousMethod))]
        [NoAutomaticTrigger]
        public async Task ContinuousMethod()
        {
            var workInstances = workInstanceProvider.Get();
            var stopwatch = new Stopwatch();
            while (true)
            {
                foreach (var work in workInstances)
                {
                    if (work.ShouldRun())
                    {
                        log.LogInformation($"Run '{work.GetType().Name}'");
                        stopwatch.Start();
                        
                        try
                        {
                            await work.Run();
                        }
                        catch (Exception ex)
                        {
                            log.LogError(ex, $"Work '{work.GetType().Name}' failed");
                        }
                        stopwatch.Stop();
                        log.LogInformation($"Ran '{work.GetType().Name}' elapsed: {stopwatch.Elapsed}");
                        stopwatch.Reset();
                    }
                }
                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }
    }
}
