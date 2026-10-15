using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProblemSource.Models.Aggregates;
using ProblemSource.Services;
using ProblemSourceModule.Services;
using System.Security.Authentication;
using TrainingApi.Authorization;
using TrainingApi.Services;

namespace TrainingApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class AggregatesController : ControllerBase
    {
        private readonly IStatisticsProvider statisticsProvider;
        private readonly ICurrentUserProvider userProvider;
		private readonly IStreamingStatistics streamingStatistics;
		private readonly ILogger<AggregatesController> _logger;

        public AggregatesController(IStatisticsProvider statisticsProvider, ICurrentUserProvider userProvider, IStreamingStatistics streamingStatistics,
			ILogger<AggregatesController> logger)
        {
            this.statisticsProvider = statisticsProvider;
            this.userProvider = userProvider;
			this.streamingStatistics = streamingStatistics;
			_logger = logger;
        }

        [HttpGet]
        public async Task<IEnumerable<TrainingDayAccount>> TrainingDayAccount(int trainingId)
        {
            AssertAccess(trainingId);
            return await statisticsProvider.GetTrainingDays(trainingId);
        }

        [HttpGet]
        public async Task<IEnumerable<PhaseStatistics>> PhaseStatistics(int trainingId)
        {
            AssertAccess(trainingId);
            return await statisticsProvider.GetPhaseStatistics(trainingId);
        }

        [AtLeastRole(Roless.Admin)]
        [HttpGet]
        public async Task<List<IStreamingStatistics.Entry>> StreamingStatistics()
        {
            return await streamingStatistics.Get();
        }

        private void AssertAccess(int trainingId)
        {
            var user = userProvider.UserOrThrow;
            if (user.Role != Roles.Admin && user.Role != Roles.SuperAdmin)
            {
                if (!userProvider.UserOrThrow.Trainings.Any(kv => kv.Value.Contains(trainingId)))
                    throw new AuthenticationException($"Access denied for training {trainingId}"); // TODO: AuthorizationException
            }
        }
    }
}