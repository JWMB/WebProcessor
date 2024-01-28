using Microsoft.AspNetCore.Mvc;
using NoK;
using ProblemSourceModule.Services.ProblemGenerators;

namespace TrainingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StimuliResponseController : ControllerBase
    {
        private readonly ILogger<UsersController> log;

        public StimuliResponseController(ILogger<UsersController> logger)
        {
            log = logger;
        }

        [HttpGet]
        public async Task<IStimulus?> Get(string id, string? source = null)
        {
            var stimuliRepository = GetProblemDomain(source).StimuliRepository;
            var stim = await stimuliRepository.GetById(id);
            return stim;
        }

        [HttpGet]
        [Route("ids")]
        public async Task<List<string>> GetAllIds(string? source = null)
        {
            var stimuliRepository = GetProblemDomain(source).StimuliRepository;
            return await stimuliRepository.GetAllIds();
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] object response)
        {
            var iresponse = IProblemDomain.Deserialize<SimpleUserResponse>(response);
            var domain = GetProblemDomain(iresponse.SourceId);
            var checker = domain.SolutionChecker;
            var typedResponse = checker.Deserialize(response);
            var problem = await domain.StimuliRepository.GetById(typedResponse.Id);
            if (problem == null)
                throw new Exception("not found");
            var analysis = await checker.Check(problem, typedResponse);
            return analysis.IsCorrect ? Ok() : BadRequest();
        }

        private IProblemDomain GetProblemDomain(string? id = null)
        {
            return new NoKDomain(new NoKStimuliRepository.Config(@"C:\Users\jonas\Downloads\assignments_141094_16961\assignments_141094_16961.json"));
        }
    }
}