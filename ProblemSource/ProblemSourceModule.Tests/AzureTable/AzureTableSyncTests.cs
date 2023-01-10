using AutoFixture;
using Castle.Core.Logging;
using Common;
using Microsoft.Extensions.Logging;
using Moq;
using ProblemSource;
using ProblemSource.Models;
using ProblemSource.Models.LogItems;
using ProblemSource.Services;
using ProblemSource.Services.Storage.AzureTables;
using ProblemSource.Tests;
using ProblemSourceModule.Services.Storage;
using Shouldly;

namespace ProblemSourceModule.Tests.AzureTable
{
    public class AzureTableSyncTests : AzureTableTestBase
    {
        [SkippableTheory]
        [InlineData("training1054598.txt")]
        public async Task SyncFullTraining(string logFile)
        {
            await Init(removeAllRows: true);

            var dir = Directory.GetCurrentDirectory();
            var json = await File.ReadAllTextAsync($@"AzureTable\{logFile}");

            var jsonRoot = System.Text.Json.JsonDocument.Parse(json).RootElement;

            var logItems = jsonRoot.EnumerateArray().Select(o => LogItem.TryDeserialize(o).parsed).Cast<LogItem>();
            var splitByDay = logItems.SplitBy(o => o is EndOfDayLogItem).ToList();
            var mnemoJapanese = new MnemoJapanese(0);

            var mockClientSessionManager = new Mock<IClientSessionManager>();
            mockClientSessionManager.Setup(o => o.GetOrOpenSession(It.IsAny<string>(), It.IsAny<string?>())).Returns(new GetOrCreateSessionResult(new Session("")));

            var repoProviderFactory = new AzureTableUserGeneratedDataRepositoriesProviderFactory(AzureTableTestBase.CreateTypedTableClientFactory());

            var training = new Training { Id = 1 };

            var mockTrainingRepo = new Mock<ITrainingRepository>();
            mockTrainingRepo.Setup(o => o.Get(It.IsAny<int>())).Returns(Task.FromResult((Training?)training));

            var middleware = TestHelpers.CreateMiddleware(fixture,
                mnemoJapanese: mnemoJapanese,
                userGeneratedDataRepositoryProviderFactory: repoProviderFactory,
                clientSessionManager: mockClientSessionManager.Object,
                aggregationService: new AggregationService(fixture.Create<ILogger<AggregationService>>()),
                trainingRepository: mockTrainingRepo.Object //fixture.Build<ITrainingRepository>().With(o => o.Get(It.IsAny<int>()), Task.FromResult((Training?)training)).Create()
                );

            var repoProvider = repoProviderFactory.Create(training.Id);
            var uuid = mnemoJapanese.FromIntWithRandom(training.Id);
            var times = new List<TimeSpan>();
            foreach (var dayItems in splitByDay)
            {
                var start = DateTime.Now;
                var result = await middleware.Sync(new SyncInput { Uuid = uuid, Events = dayItems.Select(o => (object)o).ToArray(), RequestState = true });
                times.Add(DateTime.Now - start);

                result.error.ShouldBe(null);
                result.warning.ShouldBe(null);
                result.sessionToken.ShouldNotBeEmpty();

                var jState = Newtonsoft.Json.Linq.JObject.Parse(result.state);

                var trainingDay = dayItems.OfType<NewPhaseLogItem>().First().training_day;
                (await repoProvider.TrainingDays.GetAll()).Count().ShouldBe(trainingDay);
            }
            (await repoProvider.PhaseStatistics.GetAll()).Count().ShouldBe(logItems.OfType<NewPhaseLogItem>().Count());

            var total = times.Select(o => o.TotalSeconds).Sum();
            //201 seconds
        }
    }
}
