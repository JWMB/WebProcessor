using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ProblemSource;
using ProblemSourceModule.Models;
using ProblemSourceModule.Services.Storage;
using Shouldly;
using System.Net;
using TrainingApi.Tests.IntegrationHelpers;

namespace TrainingApi.Tests
{
    public class AccountsControllerTests
    {
        [Theory]
        [InlineData(null, true, HttpStatusCode.Unauthorized)]
        [InlineData(Roles.Teacher, true, HttpStatusCode.OK)]
        [InlineData(Roles.Teacher, false, HttpStatusCode.Forbidden)]
        [InlineData(Roles.Admin, false, HttpStatusCode.OK)]
        public async Task Accounts_Authorize_AdminOrTeacher(string? role, bool userIdSameAsRequested, HttpStatusCode expected)
        {
            var requestedId = "requestedId";
            var userMakingRequest = role == null ? null : new User { Email = userIdSameAsRequested ? requestedId : "", Role = role };
            var requestedUser = userIdSameAsRequested ? userMakingRequest : new User { };

            var ts = new MyTestServer(postConfigureTestServices: services => {
                services.RemoveService<IUserRepository>();
                services.AddSingleton(sp => {
                    var repo = MyTestServer.CreateAutoMocked<IUserRepository>();
                    A.CallTo(() => repo.Get(A<string>._)).ReturnsLazily((string email) => Task.FromResult((role == null ? null : new User { Email = requestedId, Role = role })));
                    return repo;
                });
            });
            var client = ts.CreateClient(userMakingRequest);

            var response = await client.GetAsync($"/api/accounts/getone?id={requestedId}");
            response.StatusCode.ShouldBe(expected);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonConvert.DeserializeObject<User>(await response.Content.ReadAsStringAsync());
                content.ShouldNotBeNull();
            }
        }

        [Theory]
        [InlineData(null, HttpStatusCode.Unauthorized)]
        [InlineData(Roles.Teacher, HttpStatusCode.Forbidden)]
        [InlineData(Roles.Admin, HttpStatusCode.OK)]
        public async Task Accounts_Authorize_AdminOnly(string? role, HttpStatusCode expected)
        {
            var ts = new MyTestServer();
            var client = ts.CreateClient(role == null ? null : new User { Role = role });

            //var responseX = await client.PostAsJsonAsync($"/api/trainings/refresh", new List<int> { 1,2,3 });

            var response = await client.GetAsync($"/api/accounts/");
            response.StatusCode.ShouldBe(expected);
        }
    }
}
