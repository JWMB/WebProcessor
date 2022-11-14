using Common.Web;
using Microsoft.OpenApi.Models;
using OldDb.Models;
using PluginModuleBase;
using TrainingApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton(sp => new OldDbRaw("Server=localhost;Database=trainingdb;Trusted_Connection=True;"));
builder.Services.AddScoped<TrainingDbContext>();
builder.Services.AddScoped<IStatisticsProvider, RecreatedStatisticsProvider>();

//var problemSourceModule = new ProblemSource.ProblemSourceModule();
//problemSourceModule.ConfigureServices(builder.Services, false);
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

app.UseEndpoints(oldDbStartup.ConfigureGraphQL); //app.UseEndpoints(x => x.MapGraphQL()); app.Map(/graphql", )

app.Run();

IPluginModule[] ConfigureProblemSource(IServiceCollection services, IConfiguration config)
{
    TypedConfiguration.ConfigureTypedConfiguration(services, config);
    ServiceConfiguration.ConfigureDefaultJwtAuth(services, config);

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