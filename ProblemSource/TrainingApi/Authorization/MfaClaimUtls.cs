using System.Security.Claims;
//using OldDb.Models;

namespace TrainingApi.Authorization
{
	public static class MfaClaimUtls
    {
		public const string ClaimName = "mfa";
		public const string RequireValidationValue = "r";
		public const string ValidatedValue = "v";
		public const string NotRequiredValue = "";
		public enum MfaStatus
		{
			None,
			ValidationRequired,
			ValidationPassed
		}
        public static Claim CreateClaim(MfaStatus status)
        {
            return new Claim(ClaimName, status switch { MfaStatus.None => NotRequiredValue, MfaStatus.ValidationPassed => ValidatedValue, _ => RequireValidationValue, });
		}
	}
}
