using AutoFixture.AutoMoq;
using AutoFixture;
using ProblemSource.Services.Storage.AzureTables;
using Azure.Data.Tables;
using Common.Web.Services;
using Common.Web;

namespace ProblemSourceModule.Tests.AzureTable
{
    public class AzureTableTestBase
    {
        protected readonly IFixture fixture;
        protected readonly TypedTableClientFactory tableClientFactory;

        public AzureTableTestBase()
        {
            fixture = new Fixture().Customize(new AutoMoqCustomization() { ConfigureMembers = true });

            tableClientFactory = new TypedTableClientFactory(new AzureTableConfig { ConnectionString = "UseDevelopmentStorage=true", TablePrefix = "vektorTEST" });
        }

        protected virtual async Task Init()
        {
            Skip.If(!System.Diagnostics.Debugger.IsAttached);
            await tableClientFactory.Init();
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
