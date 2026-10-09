using Common.Web;
using Common.Web.Services;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using PluginModuleBase;
using ProblemSource.Services;
using ProblemSourceModule.Services;
using System.Data;
using TrainingApi.ErrorHandling;
using TrainingApi.Services;

namespace TrainingApi
{
    public class Startup
    {
        private IPluginModule[] plugins = Array.Empty<IPluginModule>();
        //private OldDbAdapter.Startup? oldDbStartup = null;
        private RealTime.Startup? realTimeStartup;
        private StartupAuthentication startupAuthentication = new();

        public void ConfigureServices(IServiceCollection services, ConfigurationManager configurationManager, IWebHostEnvironment env)
        {
            services.AddScoped<IStatisticsProvider, TempFixDuplicatesStatisticsProvider>(); // StatisticsProvider


            services.AddSingleton<CreateUserWithTrainings>();
            
            services.AddSingleton<ICookieProtector, CookieProtector>(); // NullCookieProtector


			var appSettings = TypedConfiguration.ConfigureTypedConfiguration<AppSettings>(services, configurationManager, "AppSettings");

            if (appSettings.RealTime.Enabled)
            {
                realTimeStartup = new RealTime.Startup();
                realTimeStartup.ConfigureServices(services);
            }

            startupAuthentication.ConfigureServices(services, configurationManager, env);

			plugins = ConfigureProblemSource(services, configurationManager, env);

            var oldDbEnabled = configurationManager.GetValue<bool>("OldDbEnabled");
            if (oldDbEnabled && System.Diagnostics.Debugger.IsAttached)
            {
                throw new NotImplementedException();
                //oldDbStartup = new OldDbAdapter.Startup();
                //oldDbStartup.ConfigureServices(services);
            }

            services.AddControllers();
            // Note: this can be used to customize 400 handling:
                //.ConfigureApiBehaviorOptions(options =>
                //{
                //    options.InvalidModelStateResponseFactory = context =>
                //        new BadRequestObjectResult(context.ModelState)
                //        {
                //            ContentTypes =
                //            {
                //                // using static System.Net.Mime.MediaTypeNames;
                //                Application.Json,
                //                Application.Xml
                //            }
                //        };
                //})
                //.AddXmlSerializerFormatters();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            services.AddEndpointsApiExplorer();
            AddSwaggerGen(services);
            services.AddSwaggerDocument();

            services.AddLogging(builder =>
            {
                builder.AddApplicationInsights(); // AddAzureWebAppDiagnostics
			});

            services.Configure<TelemetryConfiguration>(telemetryConfiguration =>
            {
                var builder = telemetryConfiguration.DefaultTelemetrySink.TelemetryProcessorChainBuilder;
                telemetryConfiguration.DefaultTelemetrySink.TelemetryProcessorChainBuilder
                    .UseAdaptiveSampling(maxTelemetryItemsPerSecond:5, excludedTypes: "Trace;Request;Exception");
            });
            services.AddApplicationInsightsTelemetry(new ApplicationInsightsServiceOptions
            {
                EnableAdaptiveSampling = false,
            });

            services.AddSingleton<ITelemetryInitializer, UserInformationTelemetryInitializer>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            ServiceConfiguration.ConfigurePlugins(app, plugins);

            // Configure the HTTP request pipeline.
            if (env.HasDevelopmentEnvironment())
            {
                //app.UseSwagger();
                //app.UseSwaggerUI();
                app.UseOpenApi();
                app.UseSwaggerUi();

                //app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseExceptionHandler(err => err.UseCustomErrors(env));

            app.UseCookiePolicy(new CookiePolicyOptions
            {
                Secure = CookieSecurePolicy.Always
            });

            Console.WriteLine($"EnvironmentName={env.EnvironmentName}");
			Console.WriteLine($"HasDevelopmentEnvironment={env.HasDevelopmentEnvironment()}");

			if (!env.HasEnvironmentPart("Docker"))
                app.UseHttpsRedirection();

            if (app is WebApplication webApp)
            {
                webApp.MapControllers();
                realTimeStartup?.Configure(webApp, "/realtime");
            }

            try
            {
                // TODO: separate into a method
                // static files with fallback to index.html (entry point for admin interface)
                var cacheMaxAge = TimeSpan.FromMinutes(10);
                var fileProvider = new FallbackFileProvider("index.html", new PhysicalFileProvider(Path.Combine(env.ContentRootPath, "StaticFiles", "Admin")), "/admin");
                app.UseStaticFiles(new StaticFileOptions
                {
                    ServeUnknownFileTypes = true,
                    FileProvider = fileProvider,
                    RequestPath = fileProvider.RootPath,
                    OnPrepareResponse = ctx =>
                    {
                        ctx.Context.Response.Headers.Append("Cache-Control", $"public, max-age={(int)cacheMaxAge.TotalSeconds}");
                    }
                });
            }
            catch (DirectoryNotFoundException dEx)
            {
                Console.WriteLine($"Static files folder not found: {dEx.Message}");
            }


            app.UseAuthentication();

			startupAuthentication.Configure(app, env);

			//app.UseRouting(); // Needed for GraphQL?

			var config = app.ApplicationServices.GetRequiredService<IConfiguration>();
            // With endpoint routing, the CORS middleware must be configured to execute between the calls to UseRouting and UseEndpoints.
            // Incorrect configuration will cause the middleware to stop functioning correctly.
            app.UseCors(cb =>
                cb
                    .WithOrigins((config.GetValue("CorsOrigins", "") ?? "").Split(','))
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
            );

            app.UseAuthorization();

            app.UseCookiePolicy(new CookiePolicyOptions
            {
                // TODO: is this needed? We also have it in AddCookie() below.
                MinimumSameSitePolicy = env.IsDevelopment() ? Microsoft.AspNetCore.Http.SameSiteMode.None : Microsoft.AspNetCore.Http.SameSiteMode.Lax
            });

            ServiceConfiguration.ConfigureApplicationInsights(app, config, env.IsDevelopment());

            //if (oldDbStartup != null)
            //    oldDbStartup.Configure(app);
        }

        private IPluginModule[] ConfigureProblemSource(IServiceCollection services, IConfiguration config, IHostEnvironment env)
        {
            //TypedConfiguration.ConfigureTypedConfiguration<AppSettings>(services, config, "AppSettings");
            //ConfigureAuthentication(services, config, env);

            var plugins = new IPluginModule[] { new ProblemSource.ProblemSourceModule() };
            services.AddSingleton<ITableClientFactory, TableClientFactory>();
            ServiceConfiguration.ConfigureProcessingPipelineServices(services, config, plugins);
            return plugins;
        }

        private void AddSwaggerGen(IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                //var apiinfo = new OpenApiInfo
                //{
                //    Title = "theta-CandidateAPI",
                //    Version = "v1",
                //    Description = "Candidate API for thetalentbot",
                //    Contact = new OpenApiContact { Name = "thetalentbot", Url = new Uri("https://thetalentbot.com/developers/contact") },
                //    License = new OpenApiLicense() { Name = "Commercial", Url = new Uri("https://thetalentbot.com/developers/license") }
                //};
                //c.SwaggerDoc(apiinfo.Version, apiinfo);
                // https://dev.to/timothymcgrath/til-generate-required-optional-parameters-with-nswag-3g61
                //services.AddOpenApiDocument(settings =>
                //{
                //    settings.DefaultReferenceTypeNullHandling = NJsonSchema.Generation.ReferenceTypeNullHandling.NotNull;
                //});

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
        }
    }
}
