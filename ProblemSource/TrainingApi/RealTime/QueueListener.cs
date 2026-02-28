using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Newtonsoft.Json.Linq;
using ProblemSource.Services;
using System.Text;
using TrainingApi.Services;

namespace TrainingApi.RealTime
{
    public interface IQueueMessage
    {
		string MessageId { get; }
        string PopReceipt { get; }
        BinaryData Body { get; }
	}

    public interface IQueueClient
    {
        Task<IQueueMessage[]> ReceiveMessagesAsync(int? maxMessages = null, TimeSpan? visibilityTimeout = null, CancellationToken cancellationToken = default);
        Task<bool> DeleteMessageAsync(string messageId, string popReceipt, CancellationToken cancellationToken = default);
	}

    public class AzureQueueClient : IQueueClient
    {
        private QueueClient client;

        public AzureQueueClient(AzureQueueConfig config)
        {
			if (string.IsNullOrEmpty(config.ConnectionString))
				throw new ArgumentException("null or empty", nameof(config.ConnectionString));

			if (string.IsNullOrEmpty(config.QueueName))
				throw new ArgumentException("null or empty", nameof(config.QueueName));

			client = new QueueClient(config.ConnectionString, config.QueueName); // "UseDevelopmentStorage=true", "problemsource-sync");
			// TODO: move to some async Init 
			client.CreateIfNotExists();
		}

		public async Task<bool> DeleteMessageAsync(string messageId, string popReceipt, CancellationToken cancellationToken = default)
        {
            var result = await client.DeleteMessageAsync(messageId, popReceipt, cancellationToken);
            return result.Status < 400;
        }

        public async Task<IQueueMessage[]> ReceiveMessagesAsync(int? maxMessages = null, TimeSpan? visibilityTimeout = null, CancellationToken cancellationToken = default)
        {
			var result = await client.ReceiveMessagesAsync(maxMessages, visibilityTimeout, cancellationToken);
            if (result.Value == null)
                throw new Exception("");
            return result.Value.Select(o => new WrappedMessage(o)).ToArray();
        }

        public class WrappedMessage : IQueueMessage
        {
            private readonly QueueMessage message;

            public WrappedMessage(QueueMessage message)
            {
                this.message = message;
            }
            public string MessageId => message.MessageId;
            public string PopReceipt => message.PopReceipt;
            public BinaryData Body => message.Body;

		}

    }

    public class QueueListener
    {
        private readonly IQueueClient client;
        private readonly CommHubWrapper chatHub;
        private readonly IAccessResolver accessResolver;
        private readonly ILogger<QueueListener> log;

        public QueueListener(CommHubWrapper chatHub, IAccessResolver accessResolver, RealTimeConfig config, IQueueClient queueClient, ILogger<QueueListener> log)
        {
            client = queueClient;

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

        private bool isWorking;

        public async Task Receive(CancellationToken cancellationToken)
        {
            if (isWorking) // For now at least, don't do work in parallel (messages might be sent out-of-order)
                return;

            isWorking = true;

            var start = DateTimeOffset.UtcNow;
            var processedCnt = 0;

            try
            {
                var msgs = await client.ReceiveMessagesAsync(32, cancellationToken: cancellationToken);
                //var msgs = response.Value;
                foreach (var msg in msgs)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    await ProcessMessage(msg);
                    processedCnt++;

                    try
                    {
                        await client.DeleteMessageAsync(msg.MessageId, msg.PopReceipt, cancellationToken);
                    }
                    catch (Exception inner)
                    {
                        log.LogInformation(inner, "Delete failed");
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Receive error");
            }

            if (processedCnt > 0)
                log.LogInformation($"Processed {processedCnt} msg in {DateTimeOffset.UtcNow - start}");

            isWorking = false;
        }

        private async Task ProcessMessage(IQueueMessage msg)
        {
            JObject? jObj = null;
            try
            {
                jObj = ParseMessage(msg.Body);
            }
            catch (Exception ex)
            {
                log.LogError($"Parse error: {msg.Body}", ex);
            }

            if (jObj != null)
            {
                var typed = jObj.ToObject<TrainingSyncMessage>(); // TODO: we really just want to know if we can parse to ITrainingMessage
                if (typed != null)
                {
                    // find which clients should receive events:
                    var sendToIds = chatHub.UserConnections
                        .Where(kv => accessResolver.HasAccess(kv.Value, typed.TrainingId, AccessLevel.Read))
                        .Select(kv => kv.Key)
                        .ToList();

                    if (sendToIds.Any())
                    {
                        await chatHub.Hub.Clients.Clients(sendToIds).ReceiveMessage(jObj.ToString());
                    }
                }
            }
        }

    }
}
