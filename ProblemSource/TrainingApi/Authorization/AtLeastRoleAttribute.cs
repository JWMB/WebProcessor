
using Microsoft.AspNetCore.Authorization;

namespace TrainingApi.Authorization
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
	public class AtLeastRoleAttribute : Attribute, IAuthorizationRequirementData, IAuthorizationRequirement
	{
		public bool SkipMfaCheck { get; }

		public Roless Role { get; }

		public AtLeastRoleAttribute(Roless role, bool SkipMfaCheck = false)
		{
			Role = role;
			this.SkipMfaCheck = SkipMfaCheck;
		}

		// This method implicitly registers the requirement when the attribute is evaluated
		public IEnumerable<IAuthorizationRequirement> GetRequirements() { yield return this; }
	}

	public class AtLeastRoleAuthorizationHandler : AuthorizationHandler<AtLeastRoleAttribute>
	{
		public AtLeastRoleAuthorizationHandler() { }

		protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AtLeastRoleAttribute requirement)
		{
			var user = context.User;
			if (user?.Identity?.IsAuthenticated == true)
			{
				//		if (context.User.FindAll("permission").Select(c => c.Value).Contains(requirement.Permission))
				if (user.AtLeastRole($"{requirement.Role}"))
				{
					if (requirement.SkipMfaCheck || user.MfaAuthorized() == true)
						context.Succeed(requirement);
				}
			}
			return Task.CompletedTask;


		}
	}
}
