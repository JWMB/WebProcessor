using Azure;
using Azure.Data.Tables;
using Common;
using ProblemSource.Services.Storage.AzureTables;
using ProblemSource.Services.Storage.AzureTables.TableEntities;

namespace ProblemSourceModule.Services.Storage.AzureTables
{
    public class AzureTableUserRepository : IUserRepository
    {
        private readonly TableEntityRepository<User, TableEntity> repo;
        private readonly ExpandableTableEntityConverter<User> converter;
        private readonly TableClient tableClient;

        public AzureTableUserRepository(ITypedTableClientFactory tableClientFactory)
        {
            var staticPartitionKey = "none";
            tableClient = tableClientFactory.Users;
            converter = new ExpandableTableEntityConverter<User>(t => new TableFilter(staticPartitionKey, ConvertToKey(t.Email)));
            repo = new TableEntityRepository<User, TableEntity>(tableClient, converter.ToPoco, converter.FromPoco, new TableFilter(staticPartitionKey));
        }

        private string ConvertToKey(string email) => TableKeyEncoding.Encode(email);
        //private string ConvertFromKey(string key) => TableKeyEncoding.Decode(key);

        public async Task<User?> Get(string email) => await repo.Get(ConvertToKey(email));

        private object _lock = new object();
        public Task<string> Add(User item)
        {
            // Warning: multi-instance concurrency 
            lock (_lock)
            {
                // -		ex	Count = 1	System.Exception {System.AggregateException}
                // +		InnerException	{"The specified entity already exists.\nRequestId:8570ae8a-d9ce-4523-ad65-3949c1a10b16\nTime:2023-01-05T11:51:05.5942785Z\r\nStatus: 409 (Conflict)\r\nErrorCode: EntityAlreadyExists\r\n\r\nContent:\r\n{\"odata.error\":{\"code\":\"EntityAlreadyExists\",\"message\":{\"lang\":\"sv-SE\",\"value\":\"The specified entity already exists.\\nRequestId:8570ae8a-d9ce-4523-ad65-3949c1a10b16\\nTime:2023-01-05T11:51:05.5942785Z\"}}}\r\n\r\nHeaders:\r\nCache-Control: no-cache\r\nTransfer-Encoding: chunked\r\nServer: Windows-Azure-Table/1.0,Microsoft-HTTPAPI/2.0\r\nx-ms-request-id: 8570ae8a-d9ce-4523-ad65-3949c1a10b16\r\nx-ms-version: REDACTED\r\nX-Content-Type-Options: REDACTED\r\nPreference-Applied: REDACTED\r\nDate: Thu, 05 Jan 2023 11:51:05 GMT\r\nContent-Type: application/json; odata=minimalmetadata; streaming=true; charset=utf-8\r\n"}	System.Exception {Azure.RequestFailedException}
                try
                {
                    return Task.FromResult(repo.Add(item).Result);
                }
                catch (Exception ex)
                {
                    if (ex.IsOrContains<RequestFailedException>(out var rfEx))
                    {
                        if (rfEx!.Status == 409)
                        {
                            throw rfEx;
                        }
                    }
                    throw;
                }
            }
        }

        public async Task<string> Upsert(User item) => await repo.Upsert(item);

        public async Task Update(User item) => await repo.Update(item);
        public async Task Remove(User item) => await repo.Remove(item);

        public async Task<IEnumerable<User>> GetAll() => await repo.GetAll();
    }
}
