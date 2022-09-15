using Microsoft.Extensions.DependencyInjection;

namespace PluginModuleBase
{
    public interface IPluginModule
    {
        void ConfigureServices(IServiceCollection services);
        void Configure(IServiceProvider serviceProvider);
    }
}
