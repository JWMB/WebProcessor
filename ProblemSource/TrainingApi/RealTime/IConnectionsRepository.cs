using System.Collections.Concurrent;
using System.Security.Claims;

namespace TrainingApi.RealTime
{
    public interface IConnectionsRepository
    {
        void Add(string connectionId, ClaimsPrincipal? user);
        void Remove(string connectionId);
        IDictionary<string, ClaimsPrincipal?> Connections { get; }
    }

    public class ConnectionsRepository : IConnectionsRepository
    {
        private ConcurrentDictionary<string, ClaimsPrincipal?> connections = new();
        private object lockObject = new();

        public void Add(string connectionId, ClaimsPrincipal? user)
        {
            connections.AddOrUpdate(connectionId, user, (key, val) => user);
        }
        public void Remove(string connectionId)
        {
            _ = connections!.Remove(connectionId, out _);
        }

        public IDictionary<string, ClaimsPrincipal?> Connections
        {
            get
            {
                lock (lockObject)
                {
                    return connections.ToDictionary(o => o.Key, o => o.Value);
                }
            }
        }
    }
}
