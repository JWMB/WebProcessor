using Microsoft.AspNetCore.SignalR;
using ProblemSourceModule.Models;
using TrainingApi.Services;

namespace TrainingApi.RealTime
{
    public interface ICommHub
    {
        Task ReceiveMessage(string user, string message);
    }

    public class CommHubWrapper
    {
        private readonly IHubContext<CommHub, ICommHub> chatHub;
        private readonly IConnectionsRepository connections;
        private readonly IUserProvider userResolver;

        public CommHubWrapper(IHubContext<CommHub, ICommHub> chatHub, IConnectionsRepository connections, IUserProvider userResolver)
        {
            this.chatHub = chatHub;
            this.connections = connections;
            this.userResolver = userResolver;
        }

        public IHubContext<CommHub, ICommHub> Hub => chatHub;

        public IDictionary<string, User> UserConnections => connections.Connections.ToDictionary(o => o.Key, o => userResolver.UserOrThrow); // userResolver.GetFromPrincipal(o.Value));
    }

    public class CommHub : Hub<ICommHub>
    {
        private static CommHub? instanceSingletonProtector;
        private readonly IConnectionsRepository connectionsRepository;

        public static CommHub? Instance => instanceSingletonProtector;
        public CommHub(IConnectionsRepository connectionsRepository)
        {
            if (instanceSingletonProtector != null)
                throw new Exception("Singleton only!");
            instanceSingletonProtector = this;
            this.connectionsRepository = connectionsRepository;
        }

        public async Task SendMessage(string user, string message)
        {
            await Clients.All.ReceiveMessage(user, message);
        }

        public override Task OnConnectedAsync()
        {
            connectionsRepository.Add(Context.ConnectionId, Context.User);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            connectionsRepository.Remove(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }
    }
}
