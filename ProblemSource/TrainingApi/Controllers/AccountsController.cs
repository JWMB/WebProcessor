using Microsoft.AspNetCore.Mvc;

namespace TrainingApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly ILogger<AccountsController> log;

        public AccountsController(
            ILogger<AccountsController> logger)
        {
            log = logger;
        }

        [HttpGet]
        public async Task<string> Get()
        {
            await Task.Delay(1);
            return "Hello!";
        }

        [HttpGet]
        [Route("login")]
        public ActionResult Login()
        {
            return Ok("logggg"); // Forbid();
        }
    }
}