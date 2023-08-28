using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Services.Storage;
using System.Net.Http.Headers;
using ProblemSourceModule.Models;
using AutoBogus;
using AutoBogus.FakeItEasy;
using FakeItEasy;

namespace TrainingApi.Tests.IntegrationHelpers
{
    internal class MyTestServer
    {
        public TestServer Server { get; private set; }

        public HttpClient CreateClient(User? user = null)
        {
            var client = Server.CreateClient();
            if (user != null)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AddUserMiddleware.AuthenticationScheme, System.Text.Json.JsonSerializer.Serialize(user));
           return client;
        }

        public MyTestServer(IEnumerable<User>? users = null, Action<IServiceCollection>? configureTestServices = null, Action<IServiceCollection>? postConfigureTestServices = null, Dictionary<string, string>? config = null)
        {
            Action<IServiceCollection> configure = services => {
                if (configureTestServices == null)
                {
                    configureTestServices = services =>
                    {
                        services.AddTransient<IStartupFilter, TestStartupFilter>();

                        services.AddSingleton(sp => { 
                            var repo = CreateAutoMocked<IUserRepository>();
                            A.CallTo(() => repo.Get(A<string>._)).ReturnsLazily((string uname) => Task.FromResult(users?.SingleOrDefault(o => o.Email == uname)));
                            return repo;
                        });
                        services.AddSingleton(sp => CreateAutoMocked<IUserGeneratedDataRepositoryProviderFactory>());
                        services.AddSingleton(sp => CreateAutoMocked<ITrainingRepository>());
                    };
                }

                configureTestServices(services);
                postConfigureTestServices?.Invoke(services);
            };

            Server = CreateServer(config, configure);
        }

        public static T CreateAutoMocked<T>()
            where T : class
        {
            return new AutoFaker<T>().Configure(config => config.WithBinder<FakeItEasyBinder>()).Generate();
        }

        private TestServer CreateServer(Dictionary<string, string>? config = null, Action<IServiceCollection>? configureTestServices = null)
        {
            var factory = new WebApplicationFactory<Startup>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureAppConfiguration((context, configBuilder) =>
                    {
                        configBuilder.AddJsonFile("appsettings.json");
                        if (config != null)
                            configBuilder.AddInMemoryCollection(config!);
                    });

                    builder.ConfigureTestServices(services =>
                    {
                        // TODO: Couldn't get this to work
                        //services.Configure<TestAuthHandlerOptions>(options => options.DefaultUserId = "userid");
                        //services.AddAuthentication(TestAuthHandler.AuthenticationScheme)
                        //    .AddScheme<TestAuthHandlerOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme, options => { });
                        // ... client.DefaultRequestHeaders.Add(TestAuthHandler.AuthenticationScheme, "1");

                        configureTestServices?.Invoke(services);
                    });
                });

            return factory.Server;
        }
    }
}
