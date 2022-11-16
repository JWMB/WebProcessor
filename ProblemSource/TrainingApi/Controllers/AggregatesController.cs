using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProblemSource.Models.Aggregates;
using TrainingApi.Services;

namespace TrainingApi.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class AggregatesController : ControllerBase
    {
        private readonly IStatisticsProvider statisticsProvider;
        private readonly ILogger<AggregatesController> _logger;

        public AggregatesController(IStatisticsProvider statisticsProvider,
            ILogger<AggregatesController> logger)
        {
            this.statisticsProvider = statisticsProvider;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IEnumerable<TrainingDayAccount>> TrainingDayAccount(int accountId) =>
            await statisticsProvider.GetTrainingDays(accountId);

        [HttpGet]
        public async Task<IEnumerable<PhaseStatistics>> PhaseStatistics(int accountId) =>
            await statisticsProvider.GetPhaseStatistics(accountId);
    }
}