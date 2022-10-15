using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PluginModuleBase;
using ProblemSource.Services;

namespace ProblemSource
{
    public class ProblemSourceModule : IPluginModule
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IUserStateRepository, InMemoryUserStateRepository>(); //AzureTableUserStateRepository
            //services.AddSingleton<ITrainingPlanRepository>(sp => new TrainingPlanRepository(new DirectoryInfo(sp.GetRequiredService<IHostEnvironment>().ContentRootPath)));
            services.AddSingleton<ITrainingPlanRepository, TrainingPlanRepository>();
            services.AddSingleton<ProblemSourceProcessingPipeline>();
            services.AddSingleton<IEventDispatcher, QueueEventDispatcher>(); //QueueEventDispatcher NullEventDispatcher


        }

        public void Configure(IServiceProvider serviceProvider)
        {
            serviceProvider.GetRequiredService<IProcessingPipelineRepository>().Register("problemsource", serviceProvider.GetRequiredService<ProblemSourceProcessingPipeline>());
        }
    }
}
