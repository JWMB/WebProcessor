using Common.Web.Services;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PluginModuleBase;
using System.Text;

namespace Common.Web
{
    public static class ServiceConfiguration
    {
        public static void ConfigureProcessingPipelineServices(IServiceCollection services, IEnumerable<IPluginModule> pluginModules)
        {
            services.AddSingleton<ITableClientFactory, TableClientFactory>();
            services.AddSingleton<IDataSink, AzureTableLogSink>();
            services.AddSingleton<IProcessingMiddlewarePipelineRepository, ProcessingPipelineRepository>();
            services.AddSingleton<SinkProcessingMiddleware>();

            foreach (var plugin in pluginModules)
                plugin.ConfigureServices(services);
        }

        public static void ConfigurePlugins(IServiceProvider serviceProvider, IEnumerable<IPluginModule> pluginModules)
        {
            serviceProvider.GetRequiredService<IProcessingMiddlewarePipelineRepository>().Register("default", serviceProvider.GetRequiredService<SinkProcessingMiddleware>());

            foreach (var plugin in pluginModules)
                plugin.Configure(serviceProvider);
        }

        public static void ConfigureDefaultJwtAuth(IServiceCollection services, IConfiguration config)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme,
                   cfg =>
                   {
                       // This is a way to invalidate older tokens in case of exposure
                       var issuedAfter = new DateTime(2022, 6, 15, 0, 0, 0, DateTimeKind.Utc); //DateTime.Parse(Configuration["Token:IssuedAfter"], System.Globalization.CultureInfo.InvariantCulture);
                       var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("somereallylongkeygoeshere")); //Configuration["Token:TokenSigningKey"]

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

                       //cfg.Events = new JwtBearerEvents();
                       //cfg.Events.OnAuthenticationFailed = async (cc) =>
                       //{
                       //};
                   });
        }

        public static void ConfigureApplicationInsights(IServiceProvider serviceProvider, IConfiguration config, bool isDevelopment)
        {
            var aiConn = config.GetValue("ApplicationInsights:ConnectionString", "");
            if (aiConn == "SECRET" || aiConn == string.Empty)
            {
                if (isDevelopment == false)
                    throw new ArgumentException("InstrumentationKey not set");
            }
            else
            {
                var telemetryConfig = serviceProvider.GetService<TelemetryConfiguration>();
                if (telemetryConfig != null)
                {
                    telemetryConfig.ConnectionString = aiConn;
                    var telemetry = new TelemetryClient(telemetryConfig);
                    telemetry.TrackEvent("Application start");
                    telemetry.TrackTrace("Trace Application start");
                }
            }
        }
    }
}
