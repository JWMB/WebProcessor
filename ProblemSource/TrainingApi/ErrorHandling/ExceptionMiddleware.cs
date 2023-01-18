using Microsoft.AspNetCore.Diagnostics;
using static System.Net.Mime.MediaTypeNames;

namespace TrainingApi.ErrorHandling
{
    public static class ExceptionMiddleware
    {
        public static void UseCustomErrors(this IApplicationBuilder app, IHostEnvironment environment)
        {
            if (environment.IsDevelopment())
                app.Use(WriteDevelopmentResponse);
            else
                app.Use(WriteProductionResponse);
        }

        private static Task WriteDevelopmentResponse(HttpContext httpContext, Func<Task> next)
            => WriteResponse(httpContext, includeDetails: true);

        private static Task WriteProductionResponse(HttpContext httpContext, Func<Task> next)
            => WriteResponse(httpContext, includeDetails: false);

        private static async Task WriteResponse(HttpContext context, bool includeDetails)
        {
            context.Response.ContentType = Text.Plain;
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            var message = "Internal server error";
            var exHandler = context.Features.Get<IExceptionHandlerFeature>();
            if (exHandler != null)
            {
                if (exHandler.Error is HttpException hx)
                {
                    context.Response.StatusCode = hx.StatusCode;
                    message = hx.Message;
                }
            }
            await context.Response.WriteAsync(message);
        }
    }
}
