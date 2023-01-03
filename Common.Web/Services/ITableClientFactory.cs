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
        private readonly string connectionString;
        private readonly TableClientOptions tableClientOptions;
        private readonly string tablePrefix;

        public TableClientFactory(string tablePrefix = "vektor", string? connectionString = null)
        {
            this.connectionString = string.IsNullOrEmpty(connectionString) ? "UseDevelopmentStorage=true" : connectionString;
            tableClientOptions = new TableClientOptions();
            tableClientOptions.Retry.MaxRetries = 1;
            this.tablePrefix = tablePrefix;
        }

        public TableClient CreateClient(string name) => new TableClient(connectionString, $"{tablePrefix}{name}", tableClientOptions);

        public async Task<TableClient> CreateClientAndInit(string name)
        {
            var client = CreateClient(name);
            await client.CreateIfNotExistsAsync();
            return client;
        }
    }
}
