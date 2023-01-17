using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace ProblemSource.Services
{
    public interface IEventDispatcher
    {
        public Task Dispatch(object o);
    }

    public class NullEventDispatcher : IEventDispatcher
    {
        public Task Dispatch(object o) => Task.CompletedTask;
    }

    public class QueueEventDispatcher : IEventDispatcher
    {
        private QueueClient? client;
        private readonly ILogger<QueueEventDispatcher> log;

        public QueueEventDispatcher(string connectionString, ILogger<QueueEventDispatcher> log)
        {
            var options = new QueueClientOptions();
            options.Retry.MaxRetries = 2;
            client = new QueueClient(connectionString, "problemsource-sync", options);
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
