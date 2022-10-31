using OldDb.Models;
using ProblemSource.Services.Storage;
using ProblemSource.Services.Storage.AzureTables;
using TrainingApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var tableFactory = TableClientFactory.Create().Result;
builder.Services.AddSingleton<ITableClientFactory>(sp => tableFactory);
builder.Services.AddSingleton<IUserGeneratedDataRepositoryProviderFactory, AzureTableUserGeneratedDataRepositoriesProviderFactory>();
builder.Services.AddSingleton(sp => new OldDbRaw("Server=localhost;Database=trainingdb;Trusted_Connection=True;"));
builder.Services.AddScoped<TrainingDbContext>();
builder.Services.AddScoped<IStatisticsProvider, RecreatedStatisticsProvider>();

var oldDbStartup = new OldDb.Startup();
oldDbStartup.ConfigureServices(builder.Services);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerDocument();

var app = builder.Build();

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

app.UseAuthorization();

app.MapControllers();

oldDbStartup.ConfigureEndpoints(app);

app.Run();
