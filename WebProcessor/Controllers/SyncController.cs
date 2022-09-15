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
        private readonly ILogger<SyncController> _logger;
        private readonly IProcessingPipelineRepository pipelineRepository;

        public SyncController(ILogger<SyncController> logger, IProcessingPipelineRepository pipelineRepository)
        {
            _logger = logger;
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
                    return null;
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
                return BadRequest(aEx.Message);
            }
        }
    }
}