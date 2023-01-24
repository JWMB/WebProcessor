namespace WebJob
{
    public class WorkInstanceProvider : IWorkInstanceProvider
    {
        private readonly IServiceProvider serviceProvider;

        public WorkInstanceProvider(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public IEnumerable<WorkBase> Get()
        {
            var result =  WorkBase.GetWorkTypes().Select(type => {
                WorkBase? instance = null;
                Exception? ex = null;
                try
                {
                    instance = (WorkBase)serviceProvider.CreateInstance(type);
                }
                catch (Exception e)
                {
                    ex = e;
                }
                return new { Type = type, Instance = instance, Exception = ex };
            });

            var errors = result.Where(o => o.Exception != null).Select(o => $"{o.Type.Name}: {o.Exception!.Message}").ToList();
            if (errors.Any())
            {
                throw new Exception($"Instatiating WorkBase types: {string.Join("\n", errors)}");
            }

            return result.Where(o => o.Instance != null).OfType<WorkBase>();
        }
    }
}
