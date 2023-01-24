using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;

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

    public static class IServiceProviderExtensions
    {
        // TODO: duplicate of the one in Tools
        public static T CreateInstance<T>(this IServiceProvider instance) where T : class
        {
            return (T)instance.CreateInstance(typeof(T));
        }

        public static object CreateInstance(this IServiceProvider instance, Type type)
        {
            var constructors = type.GetConstructors();

            var constructor = constructors.First();
            var parameterInfo = constructor.GetParameters();

            var parameters = parameterInfo.Select(o => instance.GetRequiredService(o.ParameterType)).ToArray();

            return constructor.Invoke(parameters);
        }
    }
}
