using ProblemSourceModule.Models;
using ProblemSourceModule.Services.Storage;
using System.Collections.Concurrent;

namespace TrainingApi.Services
{
    public interface IUserLoginService
    {
        Task<User?> GetUser(string username, string password);
    }

    public class UserLoginService : IUserLoginService
    {
        private readonly IUserRepository userRepository;

        public UserLoginService(IUserRepository userRepository)
        {
            this.userRepository = userRepository;
        }

        private static ConcurrentDictionary<string, List<DateTime>> loginAttempts = new();
        public async Task<User?> GetUser(string username, string password)
        {
            username = username.Trim().ToLower();
            password = password.Trim();
            var user = await userRepository.Get(username);
            if (user == null)
                return null;

            var maxAttempts = 5;
            if (!user.VerifyPassword(password))
            {
                loginAttempts.AddOrUpdate(username, [DateTime.UtcNow], (_, lst) => {
                    lst.Add(DateTime.UtcNow);
                    if (lst.Count > maxAttempts)
                        lst.RemoveAt(1);
                    return lst;
                });
				return null;
			}
			if (loginAttempts.TryGetValue(username, out var lst))
            {
                if (lst.Count == maxAttempts)
                {
					var sinceFirstFailed = DateTime.UtcNow - lst.First();
                    if (sinceFirstFailed.TotalMinutes < 5)
                        return null;
				}
                loginAttempts.TryRemove(username, out _);
			}

			return user;
        }
    }
}
