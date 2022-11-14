using Common.Web;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using OldDb.Models;
using PluginModuleBase;
using System.Text;
using TrainingApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton(sp => new OldDbRaw("Server=localhost;Database=trainingdb;Trusted_Connection=True;"));
builder.Services.AddScoped<TrainingDbContext>();
builder.Services.AddScoped<IStatisticsProvider, RecreatedStatisticsProvider>();

var plugins = ConfigureProblemSource(builder.Services, builder.Configuration);

var oldDbStartup = new OldDb.Startup();
oldDbStartup.ConfigureServices(builder.Services);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
AddSwaggerGen(builder.Services);
builder.Services.AddSwaggerDocument();

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

app.UseCors(cb =>
    cb
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowAnyMethod()
        .AllowAnyOrigin()
    );

app.UseHttpsRedirection();


app.MapControllers();

app.UseAuthentication();
app.UseRouting(); // Needed for GraphQL
app.UseAuthorization();
app.UseCookiePolicy(new CookiePolicyOptions { MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.Lax });

app.UseEndpoints(oldDbStartup.ConfigureGraphQL); //app.UseEndpoints(x => x.MapGraphQL()); app.Map(/graphql", )

app.Run();

IPluginModule[] ConfigureProblemSource(IServiceCollection services, IConfiguration config)
{
    TypedConfiguration.ConfigureTypedConfiguration(services, config);
    ConfigureAuth(services, config);

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

void ConfigureAuth(IServiceCollection services, IConfiguration config)
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

            //options.LoginPath = "/accounts/login";
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = 403;
                return Task.CompletedTask;
            };

            //options.AccessDeniedPath = $"/account/unauthorized";
            options.Events.OnRedirectToLogin = async (context) =>
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { LoginUrl = "https://mylogin.com" }); // TODO
            };
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            // This is a way to invalidate older tokens in case of exposure
            var issuedAfter = DateTime.Parse(config["Token:IssuedAfter"] ?? "", System.Globalization.CultureInfo.InvariantCulture);
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Token:TokenSigningKey"] ?? ""));

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = config["Token:ValidIssuer"],
                ValidAudiences = config["Token:ValidAudiences"]?.Split(',') ?? new[] { "" },

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = securityKey,

                ValidateLifetime = true,
                LifetimeValidator = (_, _, securityToken, validationParameters) =>
                    securityToken.ValidFrom > issuedAfter &&
                    securityToken.ValidTo > DateTime.UtcNow
            };
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