using Microsoft.Azure.WebJobs;

namespace WebJob
{
    public class Functions
    {
        private readonly IWorkInstanceProvider workInstanceProvider;

        public Functions(IWorkInstanceProvider workInstanceProvider)
        {
            this.workInstanceProvider = workInstanceProvider;
        }

        [Singleton]
        [FunctionName(nameof(Functions.ContinuousMethod))]
        [NoAutomaticTrigger]
        public async Task ContinuousMethod()
        {
            var workInstances = workInstanceProvider.Get();
            while (true)
            {
                foreach (var work in workInstances)
                {
                    if (work.ShouldRun())
                    {
                        await work.Run();
                    }
                }
                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }
    }
}
