using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
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
            var response = await client.GetFromJsonAsync<List<TrainingSummaryWithDaysDto>>($"/api/trainings/summaries");

            // Assert
            response!.Single().Id.ShouldBe(training.Id);
        }
    }
}
