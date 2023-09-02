using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProblemSource.Models.Aggregates;
using ProblemSource.Services;
using System.Security.Authentication;
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
        private readonly ILogger<AggregatesController> _logger;

        public AggregatesController(IStatisticsProvider statisticsProvider, ICurrentUserProvider userProvider,
            ILogger<AggregatesController> logger)
        {
            this.statisticsProvider = statisticsProvider;
            this.userProvider = userProvider;
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

        private void AssertAccess(int trainingId)
        {
            var user = userProvider.UserOrThrow;
            if (user.Role != Roles.Admin)
            {
                if (!userProvider.UserOrThrow.Trainings.Any(kv => kv.Value.Contains(trainingId)))
                    throw new AuthenticationException($"Access denied for training {trainingId}"); // TODO: AuthorizationException
            }
        }
    }
}