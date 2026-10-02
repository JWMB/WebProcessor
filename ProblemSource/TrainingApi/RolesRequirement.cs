using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
//using OldDb.Models;

namespace TrainingApi
{
    public class RolesRequirement : AuthorizationHandler<RolesRequirement>, IAuthorizationRequirement
    {
        public const string SuperAdmin = Roles.SuperAdmin; //"SuperAdmin";
        public const string Admin = Roles.Admin; //"Admin";
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
            context.User.MfaAuthorized();

			var claim = context.User.FindFirst(ClaimTypes.Role);
            if (claim != null && roles.Contains(claim.Value))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
            context.Fail();
            return Task.CompletedTask;
        }
    }

    public static class RoleUtils
    {
        public static bool AtLeastRole(string requiredRole, string? actualRole)
        {
            if (actualRole?.Any() == false)
                return false;
			var order = new[]
			{
				Roles.SuperAdmin,
				Roles.Admin,
				Roles.Teacher,
			};

			var actualIndex = order.IndexOf(actualRole);
			if (actualIndex < 0)
				return false;

			var requiredIndex = order.IndexOf(requiredRole);
			if (requiredIndex < 0)
				return false;

			return actualIndex <= requiredIndex;
		}
    }

    public static class MfaClaimUtls
    {
		public const string ClaimName = "mfa";
		public const string RequireValidationValue = "r";
		public const string ValidatedValue = "v";
		public enum MfaStatus
		{
			None,
			ValidationRequired,
			ValidationPassed
		}
        public static Claim CreateClaim(MfaStatus status)
        {
            return new Claim(ClaimName, status switch { MfaStatus.None => "", MfaStatus.ValidationPassed => ValidatedValue, _ => RequireValidationValue, });
		}
	}
	public static class ClaimsPrincipalExtensions
    {
        public static bool MfaAuthorized(this ClaimsPrincipal? principal)
        {
			var claim = principal?.FindFirst(MfaClaimUtls.ClaimName);
			if (claim == null)
				return false;
			return claim.Value == MfaClaimUtls.ValidatedValue;
        }

		public static bool AtLeastRole(this ClaimsPrincipal principal, string requiredRoleName)
        {
			var claim = principal.FindFirst(ClaimTypes.Role);
            if (claim == null)
                return false;
            return RoleUtils.AtLeastRole(requiredRoleName, claim.Value);
		}
	}
}
