using Microsoft.AspNetCore.SignalR;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProblemSourceModule.Models;
using ProblemSourceModule.Services.Storage;
using TrainingApi.Services;

namespace TrainingApi.RealTime
{
    public interface ICommHub
    {
        Task ReceiveMessage(string jsonMessage);
    }

    public class CommHubWrapper
    {
        private readonly IHubContext<CommHub, ICommHub> chatHub;
        private readonly IConnectionsRepository connections;

        public CommHubWrapper(IHubContext<CommHub, ICommHub> chatHub, IConnectionsRepository connections)
        {
            this.chatHub = chatHub;
            this.connections = connections;
        }

        public IHubContext<CommHub, ICommHub> Hub => chatHub;

        public IDictionary<string, User> UserConnections
        {
            get
            {
                return connections.Connections.Select(o => KeyValuePair.Create(o.Key, o.Value))
                    .Where(o => o.Value != null)
                    .OfType<KeyValuePair<string, User>>()
                    .ToDictionary(o => o.Key, o => o.Value); // userResolver.GetFromPrincipal(o.Value));
            }
        }
    }

    public class CommHub : Hub<ICommHub>
    {
        private static CommHub? instanceSingletonProtector;
        private readonly IConnectionsRepository connectionsRepository;
        private readonly IUserRepository userRepository;

        //public static CommHub? Instance => instanceSingletonProtector;
        public CommHub(IConnectionsRepository connectionsRepository, IUserRepository userRepository)
        {
            // TODO: gets instantiated again and again - shouldn't matter since state is preserved in injected singletons?
            //if (instanceSingletonProtector != null)
            //    throw new Exception("Singleton only!");
            //instanceSingletonProtector = this;
            this.connectionsRepository = connectionsRepository;
            this.userRepository = userRepository;
        }

        //public async Task SendMessage(object message) => await Clients.All.ReceiveMessage(message);

        public override async Task OnConnectedAsync()
        {
            var user = await WebUserProvider.GetUser(userRepository, Context.User);
            if (user == null)
                throw new Exception($"User not found: {WebUserProvider.GetNameClaim(Context.User)}");
            connectionsRepository.Add(Context.ConnectionId, user);
            await base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            connectionsRepository.Remove(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }
    }
}
