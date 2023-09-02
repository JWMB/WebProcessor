using Common.Web;
using Common.Web.Services;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using PluginModuleBase;
using ProblemSource.Services;
using System.Data;
using System.Text;
using TrainingApi.ErrorHandling;
using TrainingApi.Services;

namespace TrainingApi
{
    public class Startup
    {
        private IPluginModule[] plugins = Array.Empty<IPluginModule>();
        private OldDbAdapter.Startup? oldDbStartup = null;
        private RealTime.Startup? realTimeStartup;

        public void ConfigureServices(IServiceCollection services, ConfigurationManager configurationManager, IWebHostEnvironment env)
        {
            services.AddScoped<IStatisticsProvider, StatisticsProvider>();
            services.AddScoped<IAuthenticateUserService, AuthenticateUserService>();

            services.AddTransient<ICurrentUserProvider, WebUserProvider>();
            services.AddTransient<IAccessResolver, AccessResolver>();

            var appSettings = TypedConfiguration.ConfigureTypedConfiguration<AppSettings>(services, configurationManager, "AppSettings");

            if (appSettings.RealTime.Enabled)
            {
                realTimeStartup = new RealTime.Startup();
                realTimeStartup.ConfigureServices(services);
            }

            plugins = ConfigureProblemSource(services, configurationManager, env);

            services.AddAuthorization(options => {
                options.AddPolicy(RolesRequirement.Admin, policy => policy.Requirements.Add(new RolesRequirement(RolesRequirement.Admin)));
                options.AddPolicy(RolesRequirement.AdminOrTeacher, policy => policy.Requirements.Add(new RolesRequirement(RolesRequirement.AdminOrTeacher)));
            });

            var oldDbEnabled = configurationManager.GetValue<bool>("OldDbEnabled");
            if (oldDbEnabled && System.Diagnostics.Debugger.IsAttached)
            {
                oldDbStartup = new OldDbAdapter.Startup();
                oldDbStartup.ConfigureServices(services);
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
                builder.AddApplicationInsights();
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
            if (env.IsDevelopment())
            {
                //app.UseSwagger();
                //app.UseSwaggerUI();
                app.UseOpenApi();
                app.UseSwaggerUi3();

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

            app.UseHttpsRedirection();

            if (app is WebApplication webApp)
            {
                webApp.MapControllers();
                realTimeStartup?.Configure(webApp, "/realtime");
            }

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

            app.UseAuthentication();

            if (env.IsDevelopment())
            {
                app.Use(async (context, next) =>
                {
                    if (false && context.User?.Claims.Any() == false)
                    {
                        // For the lazy developer - swagger and test client are automatically authenticated
                        var referer = context.Request.GetTypedHeaders().Referer;
                        // when from swagger and localhost
                        var autologin = referer?.AbsolutePath.Contains("/swagger/") == true
                            || referer?.AbsoluteUri.StartsWith("http://localhost:") == true;

                        if (!autologin)
                        {
                            // when connecting for websockets (context.WebSockets.IsWebSocketRequest is false when connecting)
                            if (context.Request.Method == "CONNECT" && context.Request.Path == "/realtime")
                                autologin = true;
                        }

                        if (autologin && context.Request.Cookies.Any(o => o.Key == "autologin" && o.Value == "0"))
                            autologin = false;

                        if (autologin)
                            context.User = WebUserProvider.CreatePrincipal(WebUserProvider.FakeDevUser);
                    }

                    await next.Invoke();
                });
            }

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

            if (oldDbStartup != null)
                oldDbStartup.Configure(app);
        }

        private IPluginModule[] ConfigureProblemSource(IServiceCollection services, IConfiguration config, IHostEnvironment env)
        {
            //TypedConfiguration.ConfigureTypedConfiguration<AppSettings>(services, config, "AppSettings");
            ConfigureAuthentication(services, config, env);

            var plugins = new IPluginModule[] { new ProblemSource.ProblemSourceModule() };
            services.AddSingleton<ITableClientFactory, TableClientFactory>();
            ServiceConfiguration.ConfigureProcessingPipelineServices(services, plugins);
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

        private void ConfigureAuthentication(IServiceCollection services, IConfiguration config, IHostEnvironment env)
        {
            var combinedScheme = "JWT_OR_COOKIE";

            services.AddAuthentication(options =>
            {
                // https://weblog.west-wind.com/posts/2022/Mar/29/Combining-Bearer-Token-and-Cookie-Auth-in-ASPNET
                options.DefaultScheme = combinedScheme;
                options.DefaultChallengeScheme = combinedScheme;
                options.DefaultAuthenticateScheme = combinedScheme;
                options.DefaultForbidScheme = combinedScheme;

            }).AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.ExpireTimeSpan = TimeSpan.FromDays(1);

                //options.Cookie.SecurePolicy = true ? CookieSecurePolicy.None : CookieSecurePolicy.Always; //_environment.IsDevelopment()
                options.Cookie.SameSite = env.IsDevelopment() ? Microsoft.AspNetCore.Http.SameSiteMode.None : Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                if (Enum.TryParse< Microsoft.AspNetCore.Http.SameSiteMode>(config.GetValue("Cookies:SameSite", ""), out var sameSiteConfig))
                    options.Cookie.SameSite = sameSiteConfig;

                options.Events = new CustomCookieAuthEvents();

            }).AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                // This is a way to invalidate older tokens in case of exposure
                var issuedAfter = DateTime.Parse(config["Token:IssuedAfter"] ?? "", System.Globalization.CultureInfo.InvariantCulture);
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Token:TokenSigningKey"] ?? ""));

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = config["Token:ValidIssuer"],
                    ValidateIssuer = true,

                    ValidAudiences = config["Token:ValidAudiences"]?.Split(',') ?? new[] { "" },
                    ValidateAudience = true,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = securityKey,

                    ValidateLifetime = true,
                    LifetimeValidator = (_, _, securityToken, validationParameters) =>
                        securityToken.ValidFrom > issuedAfter &&
                        securityToken.ValidTo > DateTime.UtcNow
                };
                options.Validate();

            }).AddPolicyScheme(combinedScheme, combinedScheme, options =>
            {
                // runs on each request
                options.ForwardDefaultSelector = context =>
                {
                    // filter by auth type
                    var authorization = context.Request.Headers[HeaderNames.Authorization];
                    if (authorization.ToString().StartsWith(JwtBearerDefaults.AuthenticationScheme) == true)
                        return JwtBearerDefaults.AuthenticationScheme;

                    // otherwise always check for cookie auth
                    return CookieAuthenticationDefaults.AuthenticationScheme;
                };
            });
        }
    }
}
