using System.Reflection;

namespace WebJob
{
    public abstract class WorkBase
    {
        public abstract bool ShouldRun();
        public abstract Task Run();

        protected bool IsNightTime => DateTime.Now.Hour > 22 || DateTime.Now.Hour < 6;

        public DateTimeOffset LastRun { get; set; }
        protected TimeSpan MinInterval { get; set; } = TimeSpan.FromMinutes(10);
        protected bool MinIntervalHasPassed => (DateTimeOffset.Now - LastRun) > MinInterval;

        public static List<Type> GetWorkTypes()
        {
            //var assemblies = System.Reflection.Assembly.GetExecutingAssembly().GetReferencedAssemblies();
            var assemblies = new[] { typeof(WorkBase).Assembly };

            var type = typeof(WorkBase);
            var inherited = assemblies
                .Where(o => o.FullName != null)
                .SelectMany(o => Assembly.Load(o.FullName!).GetTypes().Where(t => type.IsAssignableFrom(t) && t.IsAbstract == false));

            return inherited.ToList();
        }
    }
}
