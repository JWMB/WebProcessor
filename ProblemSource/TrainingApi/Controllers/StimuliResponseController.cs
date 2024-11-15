using Microsoft.AspNetCore.Mvc;
using NoK;
using ProblemSourceModule.Services.ProblemGenerators;

namespace TrainingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StimuliResponseController : ControllerBase
    {
        private readonly IProblemDomainFactory problemDomainFactory;
        private readonly ILogger<UsersController> log;

        public StimuliResponseController(IProblemDomainFactory problemDomainFactory, ILogger<UsersController> logger)
        {
            this.problemDomainFactory = problemDomainFactory;
            log = logger;
        }

        [HttpGet]
        public async Task<IStimulus?> Get(string id, string? source = null)
        {
            var stimuliRepository = problemDomainFactory.GetOrThrow(source).StimuliRepository;
            var stim = await stimuliRepository.GetById(id);
            return stim;
        }

        [HttpGet]
        [Route("ids")]
        public async Task<List<string>> GetAllIds(string? source = null)
        {
            var stimuliRepository = problemDomainFactory.GetOrThrow(source).StimuliRepository;
            return await stimuliRepository.GetAllIds();
        }

        [HttpGet]
        [Route("summaries")]
        public async Task<List<object>> GetAllSummaries(string? source = null)
        {
            var stimuliRepository = problemDomainFactory.GetOrThrow(source).StimuliRepository;
            return (await stimuliRepository.GetAll())
                .Select(o =>
                {
                    var plain = INodeExtensions.HtmlToPlainText(o.Presentation);
                    return (object)new { Id = o.Id, Summary = plain.Remove(Math.Min(plain.Length - 1, 50)) };
                })
                .ToList();
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] object response)
        {
            var iresponse = IProblemDomain.DeserializeWithId<SimpleUserResponse>(response);
            var domain = problemDomainFactory.GetOrThrow(iresponse.SourceId);
            var checker = domain.GetSolutionChecker(iresponse.Id);
            var typedResponse = checker.Deserialize(response);
            var problem = await domain.StimuliRepository.GetById(typedResponse.Id);
            if (problem == null)
                throw new Exception("not found");
            var analysis = await checker.Check(problem, typedResponse);
            return analysis.IsCorrect ? Ok() : BadRequest(analysis.Feedback);
        }
    }
}