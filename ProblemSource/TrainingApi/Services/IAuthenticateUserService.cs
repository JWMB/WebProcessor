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
            var user = await userRepository.Get(username);
            if (user == null)
                return null;

            if (User.HashPassword(username, password) != user.HashedPassword)
                return null;

            return user;
        }
    }
}
