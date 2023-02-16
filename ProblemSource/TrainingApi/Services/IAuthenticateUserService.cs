using ProblemSourceModule.Models;
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

        public AuthenticateUserService(IUserRepository userRepository)
        {
            this.userRepository = userRepository;
        }

        public async Task<User?> GetUser(string username, string password)
        {
            username = username.Trim();
            password = password.Trim();
            var user = await userRepository.Get(username);
            if (user == null)
                return null;

            if (!user.VerifyPassword(password))
                return null;

            return user;
        }
    }
}
