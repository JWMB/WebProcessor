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

        public async Task Invoke(HttpContext context, RequestDelegate next)
        {
            // note: https://stackoverflow.com/a/40994711

            var body = await context.Request.ReadBodyAsStringAsync();
            if (body != null)
                await sink.Log("unknown", body);
            
            await next.Invoke(context);
        }
    }
}
