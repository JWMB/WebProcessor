using Common.Web;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using OldDb.Models;
using PluginModuleBase;
using System.Text;
using TrainingApi;
using TrainingApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton(sp => new OldDbRaw("Server=localhost;Database=trainingdb;Trusted_Connection=True;"));
builder.Services.AddScoped<TrainingDbContext>();
builder.Services.AddScoped<IStatisticsProvider, RecreatedStatisticsProvider>();

var plugins = ConfigureProblemSource(builder.Services, builder.Configuration, builder.Environment);

var oldDbStartup = new OldDb.Startup();
oldDbStartup.ConfigureServices(builder.Services);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
AddSwaggerGen(builder.Services);
builder.Services.AddSwaggerDocument();

builder.Services.AddApplicationInsightsTelemetry();
//builder.Services.AddApplicationInsightsTelemetryWorkerService();

var app = builder.Build();

//problemSourceModule.Configure(app.Services);
ServiceConfiguration.ConfigurePlugins(app.Services, plugins);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.UseSwagger();
    //app.UseSwaggerUI();
    app.UseOpenApi();
    app.UseSwaggerUi3();
}

app.UseCookiePolicy(new CookiePolicyOptions
{
    Secure = CookieSecurePolicy.Always
});

app.UseHttpsRedirection();


app.MapControllers();

app.UseAuthentication();
app.UseRouting(); // Needed for GraphQL

// With endpoint routing, the CORS middleware must be configured to execute between the calls to UseRouting and UseEndpoints.
// Incorrect configuration will cause the middleware to stop functioning correctly.
app.UseCors(cb =>
    cb
        .WithOrigins((app.Configuration.GetValue("CorsOrigins", "") ?? "").Split(','))
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
);

app.UseAuthorization();
app.UseCookiePolicy(new CookiePolicyOptions
{
    // TODO: is this needed? We also have it in AddCookie() below.
    MinimumSameSitePolicy = app.Environment.IsDevelopment() ? Microsoft.AspNetCore.Http.SameSiteMode.None : Microsoft.AspNetCore.Http.SameSiteMode.Lax
});

ServiceConfiguration.ConfigureApplicationInsights(app.Services, app.Configuration, app.Environment.IsDevelopment());

app.UseEndpoints(oldDbStartup.ConfigureGraphQL); //app.UseEndpoints(x => x.MapGraphQL()); app.Map(/graphql", )

app.Run();

IPluginModule[] ConfigureProblemSource(IServiceCollection services, IConfiguration config, IHostEnvironment env)
{
    TypedConfiguration.ConfigureTypedConfiguration(services, config);
    ConfigureAuth(services, config, env);

    var plugins = new IPluginModule[] { new ProblemSource.ProblemSourceModule() };
    ServiceConfiguration.ConfigureProcessingPipelineServices(services, plugins);
    return plugins;
}

void AddSwaggerGen(IServiceCollection services)
{
    // services.AddSwaggerGen();
    services.AddSwaggerGen(c =>
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
}

void ConfigureAuth(IServiceCollection services, IConfiguration config, IHostEnvironment env)
{
    var combinedScheme = "JWT_OR_COOKIE";
    services.AddAuthentication(options =>
    {
        // https://weblog.west-wind.com/posts/2022/Mar/29/Combining-Bearer-Token-and-Cookie-Auth-in-ASPNET

        options.DefaultScheme = combinedScheme;
        options.DefaultChallengeScheme = combinedScheme;
        options.DefaultAuthenticateScheme = combinedScheme;
        options.DefaultForbidScheme = combinedScheme;
    })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.ExpireTimeSpan = TimeSpan.FromDays(1);
            //options.Cookie.SecurePolicy = true ? CookieSecurePolicy.None : CookieSecurePolicy.Always; //_environment.IsDevelopment()
            options.Cookie.SameSite = env.IsDevelopment() ? Microsoft.AspNetCore.Http.SameSiteMode.None : Microsoft.AspNetCore.Http.SameSiteMode.Lax;

            options.Events = new CustomCookieAuthEvents();
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
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
        })
        .AddPolicyScheme(combinedScheme, combinedScheme, options =>
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