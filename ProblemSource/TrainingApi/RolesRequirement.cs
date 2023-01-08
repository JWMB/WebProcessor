using Microsoft.AspNetCore.Authorization;
using OldDb.Models;

namespace TrainingApi
{
    public class RolesRequirement : AuthorizationHandler<RolesRequirement>, IAuthorizationRequirement
    {
        public const string Admin = "Admin";
        public const string AdminOrTeacher = "AdminOrTeacher";

        private static readonly Dictionary<string, string[]> PolicyToRoles = new Dictionary<string, string[]>
        {
            { Admin, new[] { Roles.Admin } },
            { AdminOrTeacher, new[] { Roles.Admin, Roles.Teacher } }
        };

        private readonly IEnumerable<string> roles;

        public RolesRequirement(string collectionName) //IEnumerable<string> roles)
        {
            this.roles = PolicyToRoles[collectionName];
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
