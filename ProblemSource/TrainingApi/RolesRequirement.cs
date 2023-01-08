using Microsoft.AspNetCore.Authorization;

namespace TrainingApi
{
    public class RolesRequirement : AuthorizationHandler<RolesRequirement>, IAuthorizationRequirement
    {
        private readonly IEnumerable<string> roles;

        public RolesRequirement(IEnumerable<string> roles)
        {
            this.roles = roles;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RolesRequirement requirement)
        {
            var claim = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role);
            if (claim != null && roles.Contains(claim.Value))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
            context.Fail();
            return Task.CompletedTask;
        }
    }
}
