using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PluginModuleBase
{
    public interface IPluginModule
    {
        void ConfigureServices(IServiceCollection services, IConfiguration config);
		void Configure(IApplicationBuilder app);
    }
}
