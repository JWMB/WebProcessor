using Azure.Storage.Queues;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
        private readonly QueueClient client;
        public QueueEventDispatcher()
        {
            client = new QueueClient("UseDevelopmentStorage=true", "problemsource-sync");
        }

        public async Task Dispatch(object o)
        {
            try
            {
                var response = await client.CreateIfNotExistsAsync();
                if (response?.IsError == true)
                    throw new Exception($"{response.Status} {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                return;
            }

            await client.SendMessageAsync(Newtonsoft.Json.JsonConvert.SerializeObject(0));
        }
    }
}
