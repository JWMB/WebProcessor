using TrainingApi;

var startup = new Startup();
var builder = WebApplication.CreateBuilder(args);

builder.Configuration.Sources.Clear();
var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
builder.Configuration
	.AddJsonFile("appsettings.json", optional: false)
	.AddJsonFile($"appsettings.{env}.json", optional: true)
	.AddJsonFile($"appsettings.{env}-secrets.json", optional: true)
	.AddUserSecrets<Program>()
	.AddEnvironmentVariables() // prefix: "ASPNETCORE_"
	;


startup.ConfigureServices(builder.Services, builder.Configuration, builder.Environment);

var app = builder.Build();
startup.Configure(app, app.Environment);

app.Run();
