using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PluginModuleBase;
using System.Text.Json;

namespace WebApi.Controllers
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

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<object?>> Sync()
        {
            var pipelineName = User?.Claims?.FirstOrDefault(o => o.Type == "pipeline")?.Value ?? "problemsource";
            var pipeline = await pipelineRepository.Get(pipelineName);
            if (pipeline == null)
            {
                pipeline = await pipelineRepository.Get("default");
                if (pipeline == null)
                    return new EmptyResult();
            }

            // note: https://stackoverflow.com/a/40994711
            string body;
            if (Request.Body.CanSeek)
                Request.Body.Seek(0, SeekOrigin.Begin);
            using (var stream = new StreamReader(Request.Body))
            {
                body = await stream.ReadToEndAsync();
            }

            //var document = JsonDocument.Parse(body);
            try
            {
                return await pipeline.Process(body);
            }
            catch (ArgumentException aEx)
            {
                log.LogError(aEx, $"Name:{User?.Identity?.Name} Authenticated:{User?.Identity?.IsAuthenticated}");
                return BadRequest(aEx.Message);
            }
        }
    }
}