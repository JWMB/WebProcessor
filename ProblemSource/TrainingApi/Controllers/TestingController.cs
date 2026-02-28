using Microsoft.AspNetCore.Mvc;
using ProblemSourceModule.Services.TrainingAnalyzers;

namespace TrainingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestingController : ControllerBase
    {
		private readonly IPredictNumberlineLevelService predictor;
		private readonly ILogger<UsersController> log;

        public TestingController(IConfiguration configuration, IPredictNumberlineLevelService predictor, ILogger<UsersController> logger)
        {
			this.predictor = predictor;
			log = logger;
        }

        [HttpPost]
        [Route("exception")]
        public void ThrowException()
        {
            throw new NotImplementedException("Exception thrown here!");
        }

        [HttpPost]
        [Route("log")]
        public void Log([FromQuery] LogLevel level = LogLevel.Error)
        {
            log.Log(level, $"Here is a {level}");
        }

        [HttpGet("predict")]
        public async Task<IActionResult> CallPredictor()
        {
            var features = new ProblemSource.Models.Aggregates.MLFeaturesJulia
            {
                ByExercise = new Dictionary<string, ProblemSource.Models.Aggregates.MLFeaturesJulia.FeaturesForExercise>
                {
					["numberline"] = new() { FractionCorrect = 1 },
					["wm_grid"] = new() { FractionCorrect = 1 },
				}
            };
			var result = await predictor.Predict(features);
            return Ok(result);
		}
    }
}