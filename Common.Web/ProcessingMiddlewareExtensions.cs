using Microsoft.AspNetCore.Http;
using PluginModuleBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Web
{
    public static class HttpRequestExtensions
    {
        public static async Task<string?> ReadBodyAsStringAsync(this HttpRequest request)
        {
            var reqBody = request.Body;
            if (reqBody != null)
            {
                if (reqBody.CanSeek == true)
                    reqBody.Seek(0, SeekOrigin.Begin);
                using (var stream = new StreamReader(reqBody))
                {
                    return await stream.ReadToEndAsync();
                }
            }
            return null;
        }
    }
}
