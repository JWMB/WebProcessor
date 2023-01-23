using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Models;
using ProblemSourceModule.Services;
using ProblemSourceModule.Services.Storage;
using System.Reflection;

namespace WebJob
{
    public class Functions
    {
        private readonly IEnumerable<Work> workInstances;

        //public Functions(Func<IEnumerable<Work>> workInstanceProvider)
        public Functions(IEnumerable<Work> workInstances)
        {
            this.workInstances = workInstances;
        }

        [Singleton]
        [FunctionName(nameof(Functions.MyContinuousMethod))]
        [NoAutomaticTrigger]
        public async Task MyContinuousMethod()
        {
            //var workInstances = workInstanceProvider();
            while (true)
            {
                foreach (var work in workInstances)
                {
                    if (work.ShouldRun())
                    {
                        await work.Run();
                    }
                }
                await Task.Delay(10000);
            }
        }
    }

    public class WorkInstanceProvider
    {
        public IEnumerable<Work> X(IServiceProvider sp)
        {
            return Work.GetWorkTypes().Select(type => (Work)sp.CreateInstance(type));
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

    public abstract class Work
    {
        public abstract bool ShouldRun();
        public abstract Task Run();

        protected bool IsNightTime => DateTime.Now.Hour > 22 || DateTime.Now.Hour < 6;

        public DateTimeOffset LastRun { get; set; }
        protected TimeSpan MinInterval { get; set; } = TimeSpan.FromMinutes(10);
        protected bool MinIntervalHasPassed => (DateTimeOffset.Now - LastRun) > MinInterval;

        public static List<Type> GetWorkTypes()
        {
            return new List<Type> { typeof(DummyWork) };
            //var assemblies = System.Reflection.Assembly.GetExecutingAssembly().GetReferencedAssemblies();
            var assemblies = new[] { typeof(Work).Assembly };

            var type = typeof(Work);
            var inherited = assemblies
                .Where(o => o.FullName != null)
                .SelectMany(o => Assembly.Load(o.FullName!).GetTypes().Where(t => type.IsAssignableFrom(t) && t.IsAbstract == false));

            return inherited.ToList();
        }

    }

    public class DummyWork : Work
    {
        public override Task Run() => Task.CompletedTask;

        public override bool ShouldRun() => true;
    }

    public class RunDayAnalyzers : Work
    {
        private readonly TrainingAnalyzerCollection trainingAnalyzers;
        private readonly ITrainingRepository trainingRepository;
        private readonly IUserGeneratedDataRepositoryProviderFactory dataRepositoryProviderFactory;

        public RunDayAnalyzers(TrainingAnalyzerCollection trainingAnalyzers, ITrainingRepository trainingRepository, IUserGeneratedDataRepositoryProviderFactory dataRepositoryProviderFactory)
        {
            MinInterval = TimeSpan.FromHours(3);
            this.trainingAnalyzers = trainingAnalyzers;
            this.trainingRepository = trainingRepository;
            this.dataRepositoryProviderFactory = dataRepositoryProviderFactory;
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
                    var modified = await trainingAnalyzers.Execute(training, dataRepositoryProviderFactory.Create(training.Id), null);
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
