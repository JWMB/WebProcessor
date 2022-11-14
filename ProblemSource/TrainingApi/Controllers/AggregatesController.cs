using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OldDb.Models;
using ProblemSource.Models;
using ProblemSource.Models.Aggregates;
using ProblemSource.Services;
using ProblemSource.Services.Storage;
using System.Linq;
using TrainingApi.Services;
using Phase = ProblemSource.Models.Aggregates.Phase;

namespace TrainingApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
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
        [Route("TrainingDayAccount")]
        public async Task<IEnumerable<TrainingDayAccount>> TrainingDayAccount(int accountId) =>
            await statisticsProvider.GetTrainingDays(accountId);

        [HttpGet]
        [Route("PhaseStatistics")]
        public async Task<IEnumerable<PhaseStatistics>> PhaseStatistics(int accountId) =>
            await statisticsProvider.GetPhaseStatistics(accountId);
    }
}