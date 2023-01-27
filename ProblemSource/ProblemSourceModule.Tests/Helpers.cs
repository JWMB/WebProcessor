using AutoFixture;
using Microsoft.Extensions.Logging;
using PluginModuleBase;
using ProblemSource.Models;
using ProblemSource.Services.Storage;
using ProblemSource.Services;
using ProblemSourceModule.Services.Storage;
using Moq;

namespace ProblemSource.Tests
{
    public static class LogItemsExtensions
    {
        public static IEnumerable<LogItem> Prepare(this IEnumerable<LogItem> items, long? startTimeStamp = null, bool overrideExistingTime = false)
        {
            var index = 0L;
            foreach (var item in items)
            {
                item.className = item.GetType().Name;
                if (startTimeStamp != null)
                {
                    if (item.time == 0 || overrideExistingTime)
                        item.time = startTimeStamp.Value + index;
                }
                index++;
            }
            return items;
        }
    }

    public class TestHelpers
    {
        public static ProblemSourceProcessingMiddleware CreateMiddleware(IFixture fixture,
            ITrainingPlanRepository? trainingPlanRepository = null,
            IClientSessionManager? clientSessionManager = null,
            IDataSink? dataSink = null,
            IEventDispatcher? eventDispatcher = null,
            IAggregationService? aggregationService = null,
            IUserGeneratedDataRepositoryProviderFactory? userGeneratedDataRepositoryProviderFactory = null,
            UsernameHashing? usernameHashing = null,
            MnemoJapanese? mnemoJapanese = null,
            ITrainingRepository? trainingRepository = null,
            ILogger<ProblemSourceProcessingMiddleware>? logger = null
        )
        {
            return new ProblemSourceProcessingMiddleware(
                trainingPlanRepository ?? fixture.Create<ITrainingPlanRepository>(),
                clientSessionManager ?? fixture.Create<IClientSessionManager>(),
                dataSink ?? fixture.Create<IDataSink>(),
                eventDispatcher ?? fixture.Create<IEventDispatcher>(),
                aggregationService ?? fixture.Create<IAggregationService>(),
                userGeneratedDataRepositoryProviderFactory ?? fixture.Create<IUserGeneratedDataRepositoryProviderFactory>(),
                usernameHashing ?? fixture.Create<UsernameHashing>(),
                mnemoJapanese ?? fixture.Create<MnemoJapanese>(),
                trainingRepository ?? fixture.Create<ITrainingRepository>(),
                logger ?? fixture.Create<ILogger<ProblemSourceProcessingMiddleware>>()
                );
        }

        public static IUserGeneratedDataRepositoryProviderFactory MockDataRepositoryProviderFactory(
            IBatchRepository<UserGeneratedState>? userStateRepo = null,
            IUserGeneratedDataRepositoryProvider? dataProvider = null//,
            //Func<>
        )
        {
            if (userStateRepo == null)
            {
                var mockUserStateRepo = new Mock<IBatchRepository<UserGeneratedState>>();
                mockUserStateRepo.Setup(o => o.GetAll()).Returns(Task.FromResult<IEnumerable<UserGeneratedState>>(new List<UserGeneratedState>()));
                userStateRepo = mockUserStateRepo.Object;
            }

            var mockRepoProvider = new Mock<IUserGeneratedDataRepositoryProvider>();
            mockRepoProvider.Setup(o => o.UserStates).Returns(userStateRepo);

            var mockRepoProviderFactory = new Mock<IUserGeneratedDataRepositoryProviderFactory>();
            mockRepoProviderFactory.Setup(o => o.Create(It.IsAny<int>())).Returns(mockRepoProvider.Object);

            return mockRepoProviderFactory.Object;
        }
    }
}
