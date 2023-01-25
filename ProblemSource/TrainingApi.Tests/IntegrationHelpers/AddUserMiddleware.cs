using Microsoft.AspNetCore.Http;
using TrainingApi.Controllers;
using ProblemSourceModule.Models;
using TrainingApi.Services;

namespace TrainingApi.Tests.IntegrationHelpers
{
    public class AddUserMiddleware
    {
        private readonly RequestDelegate next;

        public static readonly string AuthenticationScheme = "Test";

        public AddUserMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var auth = context.Request.Headers.Authorization.FirstOrDefault();
            if (auth?.StartsWith(AuthenticationScheme) == true)
            {
                var user = System.Text.Json.JsonSerializer.Deserialize<User>(auth.Substring(auth.IndexOf(" ") + 1));
                if (user != null)
                    context.User = WebUserProvider.CreatePrincipal(user);
            }
            await next(context);
        }
    }
}
