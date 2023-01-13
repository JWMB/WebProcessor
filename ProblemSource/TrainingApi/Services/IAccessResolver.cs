using ProblemSourceModule.Models;

namespace TrainingApi.Services
{
    public interface IAccessResolver
    {
        bool HasAccess(int trainingId, AccessLevel level);
        bool HasAccess(User user, int trainingId, AccessLevel level);
    }

    public enum AccessLevel
    {
        None = 0,
        Read = 1,
        Write = 2
    }

    public class AccessResolver : IAccessResolver
    {
        private readonly ICurrentUserProvider userProvider;

        public AccessResolver(ICurrentUserProvider userProvider)
        {
            this.userProvider = userProvider;
        }

        public bool HasAccess(User user, int trainingId, AccessLevel level) => user.Role == Roles.Admin ? true : user.Trainings.Any(o => o.Value.Contains(trainingId));

        public bool HasAccess(int trainingId, AccessLevel level)
        {
            var user = userProvider.User;
            return user == null ? false : HasAccess(user, trainingId, level);
        }
    }
}
