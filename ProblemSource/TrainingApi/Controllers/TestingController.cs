using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace TrainingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestingController : ControllerBase
    {
        private readonly ILogger<AccountsController> log;

        public TestingController(IConfiguration configuration, ILogger<AccountsController> logger)
        {
            log = logger;
        }

        [HttpPost]
        [Route("exception")]
        public void ThrowException()
        {
            throw new NotImplementedException("Exception thrown here!");
        }

        [HttpPost]
        [Route("log")]
        public void Log([FromQuery] LogLevel level = LogLevel.Error)
        {
            log.Log(level, $"Here is a {level}");
        }
    }
}