using AutoFixture.AutoMoq;
using AutoFixture;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Services.Storage;
using System.Net.Http.Headers;

namespace TrainingApiTests.IntegrationHelpers
{
    internal class MyTestServer
    {
        private IFixture fixture;
        public TestServer Server { get; private set; }

        public HttpClient CreateClient(User? user = null)
        {
            var client = Server.CreateClient();
            if (user != null)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AddUserMiddleware.AuthenticationScheme, System.Text.Json.JsonSerializer.Serialize(user));
            return client;
        }

        public MyTestServer()
        {
            fixture = new Fixture().Customize(new AutoMoqCustomization() { ConfigureMembers = true });
            Server = X();
        }

        private TestServer X()
        {
            var factory = new WebApplicationFactory<TrainingApi.Startup>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureAppConfiguration((context, configBuilder) =>
                    {
                        configBuilder.AddJsonFile("appsettings.json");
                        //configBuilder.AddInMemoryCollection(new Dictionary<string, string?> { { "RawConfigProperty", "OverriddenValue" } });
                    });

                    builder.ConfigureTestServices(services =>
                    {
                        // TODO: Couldn't get this to work
                        //services.Configure<TestAuthHandlerOptions>(options => options.DefaultUserId = "userid");
                        //services.AddAuthentication(TestAuthHandler.AuthenticationScheme)
                        //    .AddScheme<TestAuthHandlerOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme, options => { });
                        // ... client.DefaultRequestHeaders.Add(TestAuthHandler.AuthenticationScheme, "1");

                        services.AddTransient<IStartupFilter, TestStartupFilter>();

                        services.AddSingleton(sp => fixture.Create<IUserRepository>());
                        services.AddSingleton(sp => fixture.Create<IUserGeneratedDataRepositoryProviderFactory>());
                        services.AddSingleton(sp => fixture.Create<ITrainingRepository>());
                    });
                });

            return factory.Server;
        }

        //private TestServer Y()
        //{
        //    var testServer = new TestServer(new WebHostBuilder()
        //        .ConfigureAppConfiguration((context, builder) =>
        //        {
        //            builder.AddJsonFile("appsettings.json");
        //        })
        //        .ConfigureTestServices(services =>
        //        {
        //            services.AddSingleton(sp => fixture.Create<IUserStateRepository>());
        //            services.AddSingleton(sp => fixture.Create<IUserRepository>());
        //            services.AddSingleton(sp => fixture.Create<IUserGeneratedDataRepositoryProviderFactory>());
        //            services.AddSingleton(sp => fixture.Create<ITrainingRepository>());
        //        })
        //        .UseStartup<TrainingApi.Startup>());
        //    return testServer;
        //}
    }
}
