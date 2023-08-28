using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProblemSource.Services;
using ProblemSourceModule.Models;
using ProblemSourceModule.Models.Aggregates;
using ProblemSourceModule.Services.Storage;
using Shouldly;
using System.Linq;
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
            response.TrainingsQuota.Created.ShouldBe(wrapper.ResolvedUser.Trainings.Sum(o => o.Value.Count));
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

        [Theory]
        [InlineData(50, 30, 10, 60)]
        [InlineData(60, 50, 10, 80)]
        [InlineData(120, 110, 10, 140)]
        [InlineData(120, 60, 10, 90)]
        public async Task CreateTrainingsInfo_Quotas(int numCreated, int numStartedWith5Days, int numStartedWith1Day, int expectedLimit)
        {
            // Arrange
            var trainingIds = Enumerable.Range(0, numCreated).ToList();

            var user = new User
            {
                Email = "a_teacher",
                Role = Roles.Teacher,
                Trainings = new UserTrainingsCollection("Group", trainingIds)
            };

            var summaries = trainingIds
                .Take(numStartedWith5Days).Select(id => new TrainingSummary { Id = id, TrainedDays = 5 })
                .Concat(trainingIds.Skip(numStartedWith5Days).Take(numStartedWith1Day).Select(id => new TrainingSummary { Id = id, TrainedDays = 1 }));

            var wrapper = new ImpersonationWrapper(user, trainingSummaries: summaries);
            var client = wrapper.CreateClient();
            // Act
            var response = await client.GetFromJsonAsync<CreateTrainingsInfoDto>($"/api/trainings/CreateTrainingsInfo");

            // Assert
            response.ShouldNotBeNull();
            response.TrainingsQuota.Created.ShouldBe(trainingIds.Count);
            response.TrainingsQuota.Started.ShouldBe(numStartedWith5Days + numStartedWith1Day);

            response.TrainingsQuota.Limit.ShouldBe(expectedLimit);
        }

        // TODO: test for PostGroup/createclass

        [Fact]
        public async Task X()
        {
            var trainings = new List<Training>
            {
                new Training { Id = 0 }
            };
            var user = new User { Trainings = new UserTrainingsCollection(new Dictionary<string, List<int>> { { "a", trainings.Select(o => o.Id).ToList() } }) };

            var trainingsRepo = A.Fake<ITrainingRepository>();
            A.CallTo(() => trainingsRepo.GetByIds(A<IEnumerable<int>>._))
                .ReturnsLazily((IEnumerable<int> ids) => Task.FromResult(trainings.Where(o => ids.Contains(o.Id))));

            var stats = A.Fake<IStatisticsProvider>();
            A.CallTo(() => stats.GetTrainingSummaries(A<IEnumerable<int>>._))
                .ReturnsLazily((IEnumerable<int> ids) => Task.FromResult((IEnumerable<TrainingSummary?>)new TrainingSummary?[] { }));

            var numToTransfer = 2;
            var result = await user.Trainings.RemoveUnusedFromGroups(numToTransfer, "", trainingsRepo, stats);
            result.Keys.ShouldBe(new[] { "a" });
            result.SelectMany(o => o.Value).Count().ShouldBe(numToTransfer);
        }
    }

    public class ImpersonationWrapper
    {
        public readonly User? ImpersonatedUser;
        public readonly User ActingUser;
        public User ResolvedUser => ImpersonatedUser ?? ActingUser;

        private readonly List<User> users;

        internal MyTestServer Server { get; private set; }

        public ImpersonationWrapper(User actingUser, User? impersonate = null, IEnumerable<TrainingSummary>? trainingSummaries = null)
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
                    A.CallTo(() => repo.Get(A<int>._))
                        .ReturnsLazily((int id) => Task.FromResult((Training?)new Training { Id = id }));
                    A.CallTo(() => repo.GetByIds(A<IEnumerable<int>>._))
                        .ReturnsLazily((IEnumerable<int> ids) => Task.FromResult(ids.Select(id => new Training { Id = id })));
                    return repo;
                });

                if (trainingSummaries?.Any() == true)
                {
                    services.AddSingleton(sp =>
                    {
                        var stats = MyTestServer.CreateAutoMocked<IStatisticsProvider>();
                        A.CallTo(() => stats.GetAllTrainingSummaries())
                            .Returns(Task.FromResult(trainingSummaries.ToList()));

                        A.CallTo(() => stats.GetTrainingSummaries(A<IEnumerable<int>>._))
                            .ReturnsLazily((IEnumerable<int> ids) => Task.FromResult(ids.Select(id => trainingSummaries.FirstOrDefault(ts => ts.Id == id)))); //ids.Select(id => (TrainingSummary?)new TrainingSummary { Id = id, TrainedDays = 5 })));
                        return stats;
                    });
                }
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
                    Trainings = new UserTrainingsCollection("AdminGroup", Enumerable.Range(0, 1))
                },
                new User
                {
                    Email = "ATeacher",
                    Role = Roles.Teacher,
                    Trainings = new UserTrainingsCollection("TeacherGroup", Enumerable.Range(0, 40))
                },
            };
        }
    }
}
