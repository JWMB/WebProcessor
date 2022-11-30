using Microsoft.AspNetCore.Http;

namespace PluginModuleBase
{
    public interface IProcessingMiddleware
    {
        //Task<object?> Process(object input, System.Security.Claims.ClaimsPrincipal? user);
        //bool SupportsMiddlewarePattern { get; }

        Task Invoke(HttpContext context, RequestDelegate next);
    }
}
