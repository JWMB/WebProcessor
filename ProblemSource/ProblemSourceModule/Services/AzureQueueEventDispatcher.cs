using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;

namespace ProblemSource.Services
{
    //public readonly record struct AzureQueueConfig(string ConnectionString, string QueueName);
    public class AzureQueueConfig
    {
        public string ConnectionString { get; set; } = "";
        public string QueueName { get; set; } = "";
    }

    public class AzureQueueEventDispatcher : IEventDispatcher
    {
        private QueueClient? client;
        private readonly ILogger<AzureQueueEventDispatcher> log;

        public AzureQueueEventDispatcher(AzureQueueConfig config, ILogger<AzureQueueEventDispatcher> log)
        {
            var options = new QueueClientOptions();
            options.Retry.MaxRetries = 2;
            client = new QueueClient(config.ConnectionString, config.QueueName, options);
            this.log = log;
        }

        public async Task Init()
        {
            if (client == null)
                return;

            try
            {
                var response = await client.CreateIfNotExistsAsync();
                if (response?.IsError == true)
                {
                    client = null;
                    log.LogError($"{response.Status} {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                client = null;
                log.LogError(ex, "Initializing client");
            }
        }

        public async Task Dispatch(object o)
        {
            if (client == null)
                return;

            try
            {
                await client.SendMessageAsync(Newtonsoft.Json.JsonConvert.SerializeObject(o), timeToLive: TimeSpan.FromMinutes(20));
            }
            catch (Exception ex)
            {
                log.LogError(ex, "SendMessageAsync");
            }
        }
    }
}
