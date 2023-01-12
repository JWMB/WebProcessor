using Azure.Storage.Queues;
using Microsoft.AspNetCore.SignalR;
using TrainingApi.Services;

namespace TrainingApi.RealTime
{
    public class QueueListener
    {
        private readonly QueueClient client;
        private readonly CommHubWrapper chatHub;
        private readonly IAccessResolver accessResolver;

        public QueueListener(CommHubWrapper chatHub, IAccessResolver accessResolver)
        {
            client = new QueueClient("UseDevelopmentStorage=true", "problemsource-sync");
            this.chatHub = chatHub;
            this.accessResolver = accessResolver;
        }

        public async Task Receive(CancellationToken cancellationToken)
        {
            try
            {
                var response = await client.ReceiveMessagesAsync(cancellationToken);
                var msgs = response.Value;
                foreach (var msg in msgs)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var trainingId = 1;
                    var body = System.Text.Encoding.UTF8.GetString(msg.Body);

                    // find which clients should receive events
                    var sendToIds = chatHub.UserConnections
                        .Where(kv => accessResolver.HasAccess(kv.Value, trainingId, AccessLevel.Read))
                        .Select(kv => kv.Key);

                    await chatHub.Hub.Clients.Clients(sendToIds).ReceiveMessage(trainingId.ToString(), body);

                    client.DeleteMessage(msg.MessageId, msg.PopReceipt);
                }
            }
            catch (Exception ex)
            { }
        }
    }
}
