using ProblemSourceModule.Models;
using System.Collections.Concurrent;

namespace TrainingApi.RealTime
{
    public interface IConnectionsRepository
    {
        void Add(string connectionId, User? user);
        void Remove(string connectionId);
        IDictionary<string, User?> Connections { get; }
    }

    public class ConnectionsRepository : IConnectionsRepository
    {
        private ConcurrentDictionary<string, User?> connections = new();
        private object lockObject = new();

        public void Add(string connectionId, User? user)
        {
            if (connections.ContainsKey(connectionId))
                return;
            // TODO: check if user already connected, then... remove old entry and add new?
            connections.AddOrUpdate(connectionId, user, (key, val) => user);
        }

        public void Remove(string connectionId)
        {
            _ = connections!.Remove(connectionId, out _);
        }

        public IDictionary<string, User?> Connections
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
