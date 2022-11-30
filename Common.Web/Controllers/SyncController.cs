using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PluginModuleBase;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace Common.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class SyncController : ControllerBase
    {
        private readonly ILogger<SyncController> log;
        private readonly IProcessingMiddlewarePipelineRepository pipelineRepository;

        public SyncController(ILogger<SyncController> logger, IProcessingMiddlewarePipelineRepository pipelineRepository)
        {
            log = logger;
            this.pipelineRepository = pipelineRepository;
        }

        [HttpPost]
        public async Task SyncUnauthorized()
        {
            await RunPipeline(Request.Query["pipeline"].FirstOrDefault());
        }

        [Authorize]
        [HttpPost]
        public async Task Sync()
        {
            var pipelineName = User?.Claims?.FirstOrDefault(o => o.Type == "pipeline")?.Value ?? "problemsource";
            await RunPipeline(pipelineName);
        }

        private async Task RunPipeline(string? pipelineName)
        {
            try
            {
                if (pipelineName == null)
                    throw new ArgumentException($"Pipeline not defined");

                var pipeline = await GetPipeline(pipelineName);
                if (pipeline == null)
                    throw new ArgumentException($"Pipeline not found: '{pipelineName}'");

                await pipeline.Invoke(HttpContext, ctx => Task.CompletedTask);
            }
            catch (Exception ex)
            {
                // log.LogError(aEx, $"Name:{User?.Identity?.Name} Authenticated:{User?.Identity?.IsAuthenticated}");
                HttpContext.Response.StatusCode = (int)(ex is ArgumentException ? HttpStatusCode.BadRequest : HttpStatusCode.InternalServerError);
                await HttpContext.Response.WriteAsync(ex.Message);
            }
        }

        private async Task<IProcessingMiddleware?> GetPipeline(string pipelineName)
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