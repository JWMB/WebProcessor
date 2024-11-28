using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureAppConfiguration(cb => { })
    //.ConfigureFunctionsWebApplication()
    .Build();

host.Run();
