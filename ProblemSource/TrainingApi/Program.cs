using TrainingApi;

var startup = new Startup();
var builder = WebApplication.CreateBuilder(args);

startup.ConfigureServices(builder.Services, builder.Configuration, builder.Environment);

var app = builder.Build();
startup.Configure(app, app.Environment);

app.Run();
