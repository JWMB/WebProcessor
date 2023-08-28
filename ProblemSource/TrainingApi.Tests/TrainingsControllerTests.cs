using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProblemSourceModule.Models;
using ProblemSourceModule.Services.Storage;
using Shouldly;
using System.Net.Http.Json;
using TrainingApi.Tests.IntegrationHelpers;
using static TrainingApi.Controllers.TrainingsController;

namespace TrainingApi.Tests
{
    public class TrainingsControllerTests
    {
        private const string basePath = "/api/trainings/";

        [Fact]
        public async Task GetTemplates_Serialization()
        {
            // Arrange
            var ts = new MyTestServer();

            var user = new User { Email = "tester", Role = Roles.Admin };
            var client = ts.CreateClient(user);

            // Act
            var response = await client.GetFromJsonAsync<List<TrainingTemplateDto>>($"{basePath}templates");

            // Assert
            var template = response!.Single(o => o.Name == "NumberlineTest training");
            template.Settings.trainingPlanOverrides.ShouldNotBeNull();
            template.Settings.trainingPlanOverrides!.ToString()!.ShouldContain("[[[]]"); //TODO: ShouldNotContain
        }

        [Fact]
        public async Task TrainingsSummaryDoesNotSkipWhenNoTrainedDays()
        {
            // Arrange
            var training = new Training { Id = 1 };

            var trainingRepo = A.Fake<ITrainingRepository>();
            A.CallTo(() => trainingRepo.GetAll()).Returns(new[] { training });

            var usersRepo = A.Fake<IUserRepository>();
            A.CallTo(() => usersRepo.Get(A<string>.Ignored)).Returns((User?)null);

            var ts = new MyTestServer(postConfigureTestServices: services => {
                services.AddSingleton(trainingRepo);
                services.AddSingleton(usersRepo);
            });

            var user = new User { Email = "tester", Role = Roles.Admin };

            var client = ts.CreateClient(user);

            // Act
            var response = await client.GetFromJsonAsync<List<TrainingSummaryWithDaysDto>>($"{basePath}summaries");

            // Assert
            response!.Single().Id.ShouldBe(training.Id);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CreateTrainingsInfo(bool withImpersonation)
        {
            // Arrange
            var users = ImpersonationWrapper.CreateDefaultUsers();
            var wrapper = new ImpersonationWrapper(users.Single(o => o.Role == Roles.Admin), withImpersonation ? users.Single(o => o.Role == Roles.Teacher) : null);

            var client = wrapper.CreateClient();

            // Act
            var response = await client.GetFromJsonAsync<CreateTrainingsInfoDto>(wrapper.AppendOptionalImpersonation($"{basePath}CreateTrainingsInfo"));

            // Assert
            response.ShouldNotBeNull();
            response.TrainingsQuota.Limit.ShouldBe(60);
            response.TrainingsQuota.InUse.ShouldBe(wrapper.ResolvedUser.Trainings.Sum(o => o.Value.Count));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GetGroups(bool withImpersonation)
        {
            // Arrange
            var users = ImpersonationWrapper.CreateDefaultUsers();
            var wrapper = new ImpersonationWrapper(users.Single(o => o.Role == Roles.Admin), withImpersonation ? users.Single(o => o.Role == Roles.Teacher) : null);

            var client = wrapper.CreateClient();

            // Act
            var response = await client.GetFromJsonAsync<Dictionary<string, List<TrainingSummaryDto>>>(wrapper.AppendOptionalImpersonation($"{basePath}groups"));

            // Assert
            response.ShouldNotBeNull();
            response.Keys.ShouldBe(wrapper.ResolvedUser.Trainings.Keys, ignoreOrder: true);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GetSummaries(bool withImpersonation)
        {
            // Arrange
            var users = ImpersonationWrapper.CreateDefaultUsers();
            var wrapper = new ImpersonationWrapper(users.Single(o => o.Role == Roles.Admin), withImpersonation ? users.Single(o => o.Role == Roles.Teacher) : null);

            var client = wrapper.CreateClient();
            var kvSelectedGroup = wrapper.ResolvedUser.Trainings.First();

            // Act
            var response = await client.GetFromJsonAsync<List<TrainingSummaryWithDaysDto>>(wrapper.AppendOptionalImpersonation($"{basePath}summaries?group={kvSelectedGroup.Key}"));

            // Assert
            response.ShouldNotBeNull();
            response.Count.ShouldBe(kvSelectedGroup.Value.Count);
        }
    }

    public class ImpersonationWrapper
    {
        public readonly User? ImpersonatedUser;
        public readonly User ActingUser;
        public User ResolvedUser => ImpersonatedUser ?? ActingUser;

        private readonly List<User> users;

        internal MyTestServer Server { get; private set; }

        public ImpersonationWrapper(User actingUser, User? impersonate = null)
        {
            ActingUser = actingUser;
            ImpersonatedUser = impersonate;

            users = new List<User> { actingUser };
            if (ImpersonatedUser != null)
                users.Add(ImpersonatedUser);

            Server = new MyTestServer(users, postConfigureTestServices: services =>
            {
                services.RemoveAll(typeof(ITrainingRepository));
                services.AddSingleton(sp =>
                {
                    var repo = MyTestServer.CreateAutoMocked<ITrainingRepository>();
                    A.CallTo(() => repo.Get(A<int>._)).ReturnsLazily((int id) => Task.FromResult((Training?)new Training { Id = id }));
                    A.CallTo(() => repo.GetByIds(A<IEnumerable<int>>._)).ReturnsLazily((IEnumerable<int> ids) => Task.FromResult(ids.Select(id => new Training { Id = id })));
                    return repo;
                });
            });
        }

        public string AppendOptionalImpersonation(string path) =>
            path + (ImpersonatedUser != null ? $"{(path.Contains("?") ? "&" : "?")}impersonate={ImpersonatedUser.Email}" : "");

        public HttpClient CreateClient() => Server.CreateClient(ActingUser);

        public static List<User> CreateDefaultUsers()
        {
            return new List<User> {
                new User
                {
                    Email = "AnAdmin",
                    Role = Roles.Admin,
                    Trainings = new Dictionary<string, List<int>> { { "AdminGroup", Enumerable.Range(0, 1).ToList() } }
                },
                new User
                {
                    Email = "ATeacher",
                    Role = Roles.Teacher,
                    Trainings = new Dictionary<string, List<int>> { { "TeacherGroup", Enumerable.Range(0, 40).ToList() } }
                },
            };
        }
    }
}
