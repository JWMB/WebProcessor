﻿using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;

namespace TrainingApi.Tests.IntegrationHelpers
{
    public class TestStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return builder =>
            {
                builder.UseMiddleware<AddUserMiddleware>();
                next(builder);
            };
        }
    }
}
