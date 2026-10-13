using System.Security.Claims;

namespace TrainingApi.Authorization
{
	public static class ClaimsPrincipalExtensions
    {
        public static bool MfaAuthorized(this ClaimsPrincipal? principal)
        {
			var claim = principal?.FindFirst(MfaClaimUtls.ClaimName);
			if (claim == null)
				return false;
			return claim.Value == MfaClaimUtls.ValidatedValue || claim.Value == MfaClaimUtls.NotRequiredValue;
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
