using ProblemSourceModule.Models;
using ProblemSourceModule.Services.Storage;
using System.Collections.Concurrent;

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

        //private static ConcurrentDictionary<string, >
        public async Task<User?> GetUser(string username, string password)
        {
            username = username.Trim();
            password = password.Trim();
            var user = await userRepository.Get(username);
            if (user == null)
                return null;

            if (!user.VerifyPassword(password))
            {
				return null;
			}

			return user;
        }
    }
}
