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
            ConfigureJwtAuth(builder.Services, builder.Configuration);

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

            builder.Services.AddSingleton<IDataSink, AzureTableLogSink>();
            builder.Services.AddSingleton<IProcessingPipelineRepository, ProcessingPipelineRepository>();
            builder.Services.AddSingleton<SinkOnlyProcessingPipeline>();

            var plugins = new IPluginModule[] { new ProblemSource.ProblemSourceModule() };

            foreach (var plugin in plugins)
                plugin.ConfigureServices(builder.Services);

            var app = builder.Build();

            app.Services.GetRequiredService<IProcessingPipelineRepository>().Register("default", app.Services.GetRequiredService<SinkOnlyProcessingPipeline>());

            foreach (var plugin in plugins)
                plugin.Configure(app.Services);

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

            var aiConn = app.Configuration.GetValue("ApplicationInsights:ConnectionString", "");
            if (aiConn == "SECRET" || aiConn == string.Empty)
            {
                if (app.Environment.IsDevelopment() == false)
                    throw new ArgumentException("InstrumentationKey not set");
            }
            else
            {
                try
                {
                    var telemetryConfig = app.Services.GetRequiredService<TelemetryConfiguration>();
                    telemetryConfig.ConnectionString = aiConn;
                    var telemetry = new TelemetryClient(telemetryConfig);
                    telemetry.TrackEvent("Application start");
                    telemetry.TrackTrace("Trace Application start");
                }
                catch
                { }
            }

            app.Run();
        }

        static void ConfigureJwtAuth(IServiceCollection services, IConfiguration config)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme,
                   cfg =>
                   {
                        // This is a way to invalidate older tokens in case of exposure
                       var issuedAfter = new DateTime(2022, 6, 15, 0, 0, 0, DateTimeKind.Utc); //DateTime.Parse(Configuration["Token:IssuedAfter"], System.Globalization.CultureInfo.InvariantCulture);
                       var securityKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("somereallylongkeygoeshere")); //Configuration["Token:TokenSigningKey"]

                       cfg.TokenValidationParameters = new TokenValidationParameters
                       {
                           ValidIssuer = "jwmb", //Configuration["Token:ValidIssuer"],
                           ValidAudiences = new List<string>
                            {
                                "logsink_client", //Configuration["Token:ValidAudience"],
                            },

                           ValidateIssuerSigningKey = true,
                           IssuerSigningKey = securityKey,

                           ValidateLifetime = true,
                           LifetimeValidator = (_, _, securityToken, validationParameters) =>
                               securityToken.ValidFrom > issuedAfter &&
                               securityToken.ValidTo > DateTime.UtcNow
                       };
                   });
        }
    }
}
