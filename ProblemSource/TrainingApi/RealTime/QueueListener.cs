using Azure.Storage.Queues;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json.Linq;
using ProblemSource.Services.Storage.AzureTables;
using System.Text;
using TrainingApi.Services;
using YamlDotNet.Core.Tokens;

namespace TrainingApi.RealTime
{
    public class QueueListener
    {
        private readonly QueueClient client;
        private readonly CommHubWrapper chatHub;
        private readonly IAccessResolver accessResolver;
        private readonly ILogger<QueueListener> log;

        public QueueListener(CommHubWrapper chatHub, IAccessResolver accessResolver, RealTimeConfig config, ILogger<QueueListener> log)
        {
            if (config.AzureQueueConfig == null)
                throw new NullReferenceException($"{nameof(config.AzureQueueConfig)}");

            client = new QueueClient(config.AzureQueueConfig.ConnectionString, config.AzureQueueConfig.QueueName); // "UseDevelopmentStorage=true", "problemsource-sync");

            // TODO: move to some async Init 
            client.CreateIfNotExists();

            this.chatHub = chatHub;
            this.accessResolver = accessResolver;
            this.log = log;
        }

        private JObject? ParseMessage(BinaryData blob)
        {
            var body = System.Text.Encoding.UTF8.GetString(blob);
            if (!body.StartsWith("{") && !body.StartsWith("["))
            {
                // helper while developing...
                try
                {
                    body = Encoding.UTF8.GetString(System.Convert.FromBase64String(body));
                }
                catch
                {
                    return null;
                }
            }
            try
            {
                return Newtonsoft.Json.Linq.JObject.Parse(body);
            }
            catch
            {
                return null;
            }
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

                    var jObj = ParseMessage(msg.Body);

                    if (jObj != null)
                    {
                        var trainingId = jObj.Value<int?>("trainingId");
                        if (trainingId.HasValue)
                        {
                            // find which clients should receive events:
                            var sendToIds = chatHub.UserConnections
                                .Where(kv => accessResolver.HasAccess(kv.Value, trainingId.Value, AccessLevel.Read))
                                .Select(kv => kv.Key)
                                .ToList();

                            if (sendToIds.Any())
                            {
                                await chatHub.Hub.Clients.Clients(sendToIds).ReceiveMessage(jObj.ToString());
                            }
                        }
                    }

                    client.DeleteMessage(msg.MessageId, msg.PopReceipt);
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Receive error");
            }
        }
    }
}
