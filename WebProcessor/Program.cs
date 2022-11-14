using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ProblemSource.Services;
using PluginModuleBase;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Common.Web;
using Common.Web.Services;

namespace WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(
                args);

            // Add services to the container.

            TypedConfiguration.ConfigureTypedConfiguration(builder.Services, builder.Configuration);
            ServiceConfiguration.ConfigureDefaultJwtAuth(builder.Services, builder.Configuration);

            var plugins = new IPluginModule[] { new ProblemSource.ProblemSourceModule() };
            ServiceConfiguration.ConfigureProcessingPipelineServices(builder.Services, plugins);

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("jwt_auth", new OpenApiSecurityScheme()
                {
                    Name = "Bearer",
                    BearerFormat = "JWT",
                    Scheme = "bearer",
                    Description = "Specify the authorization token.",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                });

                // Make sure swagger UI requires a Bearer token specified
                var securityScheme = new OpenApiSecurityScheme()
                {
                    Reference = new OpenApiReference()
                    {
                        Id = "jwt_auth",
                        Type = ReferenceType.SecurityScheme
                    }
                };
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                    { { securityScheme, new string[] { } } }
                );
            });
            builder.Services.AddApplicationInsightsTelemetry(options =>
                options.ConnectionString = builder.Configuration.GetValue("ApplicationInsights:ConnectionString", ""));

            var app = builder.Build();

            ServiceConfiguration.ConfigurePlugins(app.Services, plugins);

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors(builder =>
                builder
                    //.WithOrigins("https://localhost:3000", "http://localhost:3000")
                    .AllowAnyHeader()
                    .AllowAnyOrigin()
                    .AllowAnyMethod());

            app.UseHttpsRedirection();

            app.UseAuthentication();
            // app.UseRouting();
            app.UseAuthorization();

            app.MapControllers();

            ServiceConfiguration.ConfigureApplicationInsights(app.Services, app.Configuration, app.Environment.IsDevelopment());

            app.Run();
        }
    }
}
