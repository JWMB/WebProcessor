using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace TrainingApi.ErrorHandling
{
    public static class ExceptionMiddleware
    {
        // https://andrewlock.net/creating-a-custom-error-handler-middleware-function/
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
            var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
            if (ex == null)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync(message);
            }
            else
            {
                context.Response.ContentType = "application/problem+json"; // ProblemDetails has it's own content type

                var problem = new ProblemDetails
                {
                    Status = 500,
                    Title = includeDetails ? "An error occured: " + ex.Message : "An error occured",
                    Detail = includeDetails ? ex.ToString() : null
                };
                if (ex is HttpException hx)
                {
                    problem.Status = hx.StatusCode;
                    problem.Title = hx.Message;
                }

                // This is often very handy information for tracing the specific request
                var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
                if (traceId != null)
                {
                    problem.Extensions["traceId"] = traceId;
                }

                //Serialize the problem details object to the Response as JSON (using System.Text.Json)
                context.Response.StatusCode = problem.Status ?? 500;
                await JsonSerializer.SerializeAsync(context.Response.Body, problem);
            }
        }
    }
}
