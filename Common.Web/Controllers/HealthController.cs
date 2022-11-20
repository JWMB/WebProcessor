using Microsoft.AspNetCore.Mvc;

namespace Common.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class HealthController : ControllerBase
    {
        private readonly Uri[] syncUrls;
        public HealthController(AppSettings appSettings)
        {
            syncUrls = string.IsNullOrEmpty(appSettings.SyncUrls)
                ? new Uri[0]
                : appSettings.SyncUrls.Split(',').Select(o => Uri.TryCreate(o.Trim(), UriKind.Absolute, out var uri) ? uri : null).OfType<Uri>().ToArray();
        }

        [HttpGet]
        public ActionResult Heartbeat()
        {
            return Ok();
        }
    }
}
