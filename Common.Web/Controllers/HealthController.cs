using Microsoft.AspNetCore.Mvc;

namespace Common.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class HealthController : ControllerBase
    {
        public HealthController()
        {
        }

        [HttpGet]
        public ActionResult Heartbeat()
        {
            return Ok();
        }
    }
}
