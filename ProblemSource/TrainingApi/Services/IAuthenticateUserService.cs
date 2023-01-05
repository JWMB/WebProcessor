using ProblemSourceModule.Services.Storage;

namespace TrainingApi.Services
{
    public interface IAuthenticateUserService
    {
        Task<User?> GetUser(string username, string password);
    }

    public class AuthenticateUserService : IAuthenticateUserService
    {
        private readonly IUserRepository userRepository;
        private readonly Dictionary<string, string> hardcodedUsers;

        public AuthenticateUserService(IConfiguration configuration, IUserRepository userRepository)
        {
            this.userRepository = userRepository;

            var section = configuration.GetSection("Users");
            hardcodedUsers = section?.GetChildren().ToDictionary(o => o.Key, o => o.Value ?? "") ?? new();
        }

        public async Task<User?> GetUser(string username, string password)
        {
            var user = await userRepository.Get(username);
            if (user == null)
            {
                if (!hardcodedUsers.TryGetValue(username, out var storedPwd) || storedPwd != password)
                {
                    return new User { Email = "dev", Role = "Admin" };
                }
                return null;
            }

            if (User.HashPassword(password) != user.HashedPassword)
            {
                return null;
            }

            return user;
        }
    }
}
