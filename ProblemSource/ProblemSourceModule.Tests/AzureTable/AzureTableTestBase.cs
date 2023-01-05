using AutoFixture.AutoMoq;
using AutoFixture;
using ProblemSource.Services.Storage.AzureTables;

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

        protected async Task Init()
        {
            Skip.If(!System.Diagnostics.Debugger.IsAttached);
            await tableClientFactory.Init();
        }

        public void EnableNonDebugSkip() => Skip.If(!System.Diagnostics.Debugger.IsAttached);

    }
}
