using AutoBogus;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Newtonsoft.Json;
using ProblemSource;
using ProblemSourceModule.Models;
using ProblemSourceModule.Services.Storage;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using TrainingApi;
using TrainingApiTests.IntegrationHelpers;
using static TrainingApi.Controllers.TrainingsController;

namespace TrainingApiTests
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

            var ts = new MyTestServer(postConfigureTestServices: services => {
                services.AddSingleton(trainingRepo);
            });

            var user = new User { Email = "email", Role = Roles.Admin };

            var client = ts.CreateClient(user);

            // Act
            var response = await client.GetFromJsonAsync<List<TrainingSummaryWithDaysDto>>($"/api/trainings/summaries");

            // Assert
            response!.Single().Id.ShouldBe(training.Id);
        }
    }
}
