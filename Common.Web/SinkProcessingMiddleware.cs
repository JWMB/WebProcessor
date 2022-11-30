using Azure.Core;
using Microsoft.AspNetCore.Http;
using PluginModuleBase;

namespace Common.Web
{
    public class SinkProcessingMiddleware : IProcessingMiddleware
    {
        private readonly IDataSink sink;

        public SinkProcessingMiddleware(IDataSink sink)
        {
            this.sink = sink;
        }

        //public bool SupportsMiddlewarePattern => true;

        public async Task Invoke(HttpContext context, RequestDelegate next)
        {
            // note: https://stackoverflow.com/a/40994711

            var body = await context.Request.ReadBodyAsStringAsync();
            if (body != null)
                await sink.Log("unknown", body);
            
            await next.Invoke(context);
        }

        //public async Task<object?> Process(object input, System.Security.Claims.ClaimsPrincipal? user)
        //{
        //    // TODO: a client and/or user id
        //    await sink.Log("unknown", input);
        //    return null;
        //}
    }
}
