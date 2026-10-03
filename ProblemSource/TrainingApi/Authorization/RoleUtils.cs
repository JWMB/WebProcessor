namespace TrainingApi.Authorization
{
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

	// public class RolesRequirement : AuthorizationHandler<RolesRequirement>, IAuthorizationRequirement
	// {
	//     public const string SuperAdmin = Roles.SuperAdmin; //"SuperAdmin";
	//     public const string Admin = Roles.Admin; //"Admin";
	//     public const string AdminOrTeacher = "AdminOrTeacher";

	//     private static readonly Dictionary<string, string[]> PolicyToRoles = new Dictionary<string, string[]>
	//     {
	//         { Admin, new[] { Roles.Admin } },
	//         { AdminOrTeacher, new[] { Roles.Admin, Roles.Teacher } }
	//     };

	//     private readonly IEnumerable<string> roles;

	//     public RolesRequirement(string collectionName) //IEnumerable<string> roles)
	//     {
	//         this.roles = PolicyToRoles[collectionName];
	//     }

	//     protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RolesRequirement requirement)
	//     {
	//         context.User.MfaAuthorized();

	//var claim = context.User.FindFirst(ClaimTypes.Role);
	//         if (claim != null && roles.Contains(claim.Value))
	//         {
	//             context.Succeed(requirement);
	//             return Task.CompletedTask;
	//         }
	//         context.Fail();
	//         return Task.CompletedTask;
	//     }
	// }
}
