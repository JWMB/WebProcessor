using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    // TODO (low): for backwards compatibility. This was used for load-balancing, but now, we'd do scalable multi-instance services with ARRAffinity instead

    [ApiController]
    //[EnableCors]
    [Route("api/[controller]/[action]")]
    public class RelayController : ControllerBase
    {
        private readonly Uri[] syncUrls;
        public RelayController(AppSettings appSettings)
        {
            syncUrls = string.IsNullOrEmpty(appSettings.SyncUrls)
                ? new Uri[0]
                : appSettings.SyncUrls.Split(',').Select(o => Uri.TryCreate(o.Trim(), UriKind.Absolute, out var uri) ? uri : null).OfType<Uri>().ToArray();
        }

        [HttpGet]
        public object GetSyncUrls([FromQuery] string uuid)
        {
            return (syncUrls.Any() ? syncUrls : new[] { new Uri($"https://{Request.Host}/api/sync/sync") })
                .Select(o => new { Url = o });
        }
    }
}
