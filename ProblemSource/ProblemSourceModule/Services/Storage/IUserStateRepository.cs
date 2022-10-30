using System.Collections.Concurrent;
using ProblemSource.Models;
using Azure;

namespace ProblemSource.Services.Storage
{
    public interface IUserStateRepository
    {
        Task Set(string uuid, object state); //IUserGeneratedState
        Task<object?> Get(string uuid);
        Task<T?> Get<T>(string uuid) where T : class;
    }

    public class InMemoryUserStateRepository : IUserStateRepository
    {
        private static ConcurrentDictionary<string, object> userStates = new ConcurrentDictionary<string, object>(); //IUserGeneratedState
        public Task Set(string uuid, object state)
        {
            userStates.AddOrUpdate(uuid, state, (s1, s2) => state);
            return Task.CompletedTask;
            //await File.WriteAllTextAsync(@"", Newtonsoft.Json.JsonConvert.SerializeObject(state));
        }

        public Task<object?> Get(string uuid) => Task.FromResult(userStates.GetValueOrDefault(uuid));

        public Task<T?> Get<T>(string uuid) where T : class => Task.FromResult(userStates.GetValueOrDefault(uuid) as T);
    }
}
