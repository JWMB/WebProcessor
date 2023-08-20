using Azure.Storage.Queues;
using Newtonsoft.Json.Linq;
using System.Text;
using TrainingApi.Services;

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

            if (string.IsNullOrEmpty(config.AzureQueueConfig.ConnectionString))
                throw new ArgumentException("null or empty", nameof(config.AzureQueueConfig.ConnectionString));

            if (string.IsNullOrEmpty(config.AzureQueueConfig.QueueName))
                throw new ArgumentException("null or empty", nameof(config.AzureQueueConfig.QueueName));

            client = new QueueClient(config.AzureQueueConfig.ConnectionString, config.AzureQueueConfig.QueueName); // "UseDevelopmentStorage=true", "problemsource-sync");

            // TODO: move to some async Init 
            client.CreateIfNotExists();

            this.chatHub = chatHub;
            this.accessResolver = accessResolver;
            this.log = log;
        }

        private JObject? ParseMessage(BinaryData blob)
        {
            var body = Encoding.UTF8.GetString(blob);
            if (!body.StartsWith("{") && !body.StartsWith("["))
            {
                // helper while developing...
                try
                {
                    body = Encoding.UTF8.GetString(Convert.FromBase64String(body));
                }
                catch
                {
                    return null;
                }
            }
            try
            {
                return JObject.Parse(body);
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
