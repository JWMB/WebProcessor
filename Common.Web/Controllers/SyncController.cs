using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PluginModuleBase;
using System.Text.Json;

namespace Common.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class SyncController : ControllerBase
    {
        private readonly ILogger<SyncController> log;
        private readonly IProcessingPipelineRepository pipelineRepository;

        public SyncController(ILogger<SyncController> logger, IProcessingPipelineRepository pipelineRepository)
        {
            log = logger;
            this.pipelineRepository = pipelineRepository;
        }

        [HttpPost]
        public async Task<ActionResult<object?>> SyncUnauthorized()
        {
            var pipelineName = Request.Query["pipeline"].FirstOrDefault();
            if (pipelineName == null)
                return BadRequest($"Pipeline not found ${pipelineName}");
            return await RunPipeline(pipelineName, User);
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<object?>> Sync()
        {
            var pipelineName = User?.Claims?.FirstOrDefault(o => o.Type == "pipeline")?.Value ?? "problemsource";
            try
            {
                return await RunPipeline(pipelineName, User);
            }
            catch (ArgumentException aEx)
            {
                log.LogError(aEx, $"Name:{User?.Identity?.Name} Authenticated:{User?.Identity?.IsAuthenticated}");
                return BadRequest(aEx.Message);
            }
        }

        private async Task<ActionResult<object?>> RunPipeline(string pipelineName, System.Security.Claims.ClaimsPrincipal? user)
        {
            var pipeline = await GetPipeline(pipelineName);
            if (pipeline == null)
            {
                return BadRequest($"Pipeline not found ${pipelineName}");
            }

            // note: https://stackoverflow.com/a/40994711
            string body;
            if (Request.Body.CanSeek)
                Request.Body.Seek(0, SeekOrigin.Begin);
            using (var stream = new StreamReader(Request.Body))
            {
                body = await stream.ReadToEndAsync();
            }

            return await pipeline.Process(body, user);
        }

        private async Task<IProcessingPipeline?> GetPipeline(string pipelineName)
        {
            var pipeline = await pipelineRepository.Get(pipelineName);
            if (pipeline == null)
            {
                pipeline = await pipelineRepository.Get("default");
            }
            return pipeline;
        }
    }
}