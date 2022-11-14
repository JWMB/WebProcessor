using Azure.Data.Tables;

namespace Common.Web.Services
{
    public interface ITableClientFactory
    {
        TableClient CreateClient(string name);
        Task<TableClient> CreateClientAndInit(string name);
    }

    public class TableClientFactory : ITableClientFactory
    {
        private readonly string prefix = "vektor";
        private readonly string connectionString;
        private readonly TableClientOptions tableClientOptions;
        public TableClientFactory(string? connectionString)
        {
            this.connectionString = string.IsNullOrEmpty(connectionString) ? "UseDevelopmentStorage=true" : connectionString;
            tableClientOptions = new TableClientOptions();
            tableClientOptions.Retry.MaxRetries = 1;
        }

        public TableClient CreateClient(string name) => new TableClient(connectionString, $"{prefix}{name}", tableClientOptions);

        public async Task<TableClient> CreateClientAndInit(string name)
        {
            var client = CreateClient(name);
            await client.CreateIfNotExistsAsync();
            return client;
        }
    }
}
