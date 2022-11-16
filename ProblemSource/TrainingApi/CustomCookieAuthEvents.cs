using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

namespace TrainingApi
{
    public class CustomCookieAuthEvents : CookieAuthenticationEvents
    {
        public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
        {
            context.Response.StatusCode = 403;
            return Task.CompletedTask;
        }

        public override async Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { LoginUrl = "https://mylogin.com" }); // TODO
        }

        //public override Task SigningIn(CookieSigningInContext context) => base.SigningIn(context);
        ////Generated new cookie information available here. In the request Body. 
        //public override Task SignedIn(CookieSignedInContext context) => base.SignedIn(context);
    }
}
