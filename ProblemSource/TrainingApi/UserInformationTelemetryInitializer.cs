using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System.Security.Claims;

namespace TrainingApi
{
    public class UserInformationTelemetryInitializer : ITelemetryInitializer
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public UserInformationTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public void Initialize(ITelemetry telemetry)
        {
            //WebTelemetryInitializerBase
            var user = httpContextAccessor.HttpContext?.User;
            if (user != null)
            {
                if (telemetry is RequestTelemetry
                    || telemetry is TraceTelemetry
                    || telemetry is ExceptionTelemetry)
                {
                    var emailClaim = user.FindFirst(ClaimTypes.Email) ?? user.FindFirst(ClaimTypes.Name); // TODO: remove Name check in next release
                    if (emailClaim != null)
                    {
                        telemetry.Context.GlobalProperties.Add("userEmail", emailClaim.Value);
                    }
                }
            }
        }
    }
}
