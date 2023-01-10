using AutoFixture.AutoMoq;
using AutoFixture;
using ProblemSource.Services.Storage.AzureTables;
using Azure.Data.Tables;

namespace ProblemSourceModule.Tests.AzureTable
{
    public class AzureTableTestBase
    {
        protected readonly IFixture fixture;
        protected readonly TypedTableClientFactory tableClientFactory;

        public AzureTableTestBase()
        {
            fixture = new Fixture().Customize(new AutoMoqCustomization() { ConfigureMembers = true });

            tableClientFactory = CreateTypedTableClientFactory();
        }

        public static TypedTableClientFactory CreateTypedTableClientFactory() =>
            new TypedTableClientFactory(new AzureTableConfig { ConnectionString = "UseDevelopmentStorage=true", TablePrefix = "vektorTEST" });

        protected virtual async Task Init(bool removeAllRows = false)
        {
            Skip.If(!System.Diagnostics.Debugger.IsAttached);
            await tableClientFactory.Init();

            if (removeAllRows)
                await RemoveAllRows();
        }

        protected async Task RemoveAllRows()
        {
            foreach (var client in tableClientFactory.AllClients())
            {
                await Services.Storage.AzureTables.AzureTableHelpers.IterateOverRows(client, "", 
                    row => new TableTransactionAction(TableTransactionActionType.Delete, row), 
                    async (_, tx) => await client.SubmitTransactionAsync(tx));
            }
        }

        public void EnableNonDebugSkip() => Skip.If(!System.Diagnostics.Debugger.IsAttached);

    }
}
