using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ProblemSource;
using ProblemSourceModule.Models;
using ProblemSourceModule.Services.Storage;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using TrainingApi.Tests.IntegrationHelpers;
using static TrainingApi.Controllers.UsersController;

namespace TrainingApi.Tests
{
    public class UsersControllerTests
    {
        private readonly string basePath = "/api/users/";

        [Theory]
        [InlineData(null, true, HttpStatusCode.Unauthorized)]
        [InlineData(Roles.Teacher, true, HttpStatusCode.OK)]
        [InlineData(Roles.Teacher, false, HttpStatusCode.Forbidden)]
        [InlineData(Roles.Admin, false, HttpStatusCode.OK)]
        public async Task User_Authorize_AdminOrTeacher(string? role, bool userIdSameAsRequested, HttpStatusCode expected)
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

            var response = await client.GetAsync($"{basePath}getone?id={requestedId}");
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
        public async Task User_Authorize_AdminOnly(string? role, HttpStatusCode expected)
        {
            var ts = new MyTestServer();
            var client = ts.CreateClient(role == null ? null : new User { Role = role });

            //var responseX = await client.PostAsJsonAsync($"/api/trainings/refresh", new List<int> { 1,2,3 });

            var response = await client.GetAsync($"{basePath}");
            response.StatusCode.ShouldBe(expected);
        }

        [Theory]
        [InlineData(new int[] { 1, 2, 4 }, HttpStatusCode.BadRequest)]
        [InlineData(new int[] { 1, 2, 3 }, HttpStatusCode.OK)]
        public async Task User_MoveTrainings(IEnumerable<int> ids, HttpStatusCode expected)
        {
            var user = new User
            {
                Role = Roles.Teacher,
                Trainings = new Dictionary<string, List<int>>
                {
                    { "a", new  List<int>{ 1, 2, 3 } },
                    { "b", new  List<int>{ 4, 5, 6 } }
                }
            };
            var ts = new MyTestServer(users: new[] { user });

            var userRepo = ts.Server.Services.GetRequiredService<IUserRepository>();

            var client = ts.CreateClient(user);

            var response = await client.PutAsJsonAsync($"{basePath}movetrainings", new MoveTrainingsDto { FromGroup = "a", ToGroup = "b", TrainingIds = ids.ToList() });
            response.StatusCode.ShouldBe(expected);

            if (response.StatusCode == HttpStatusCode.OK)
                A.CallTo(() => userRepo.Update(A<User>._)).MustHaveHappened();
            else
                A.CallTo(() => userRepo.Update(A<User>._)).MustNotHaveHappened();
        }
    }
}
