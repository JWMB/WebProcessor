using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OldDb.Models;

namespace OldDb
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<TrainingDbContext>(options => options.UseSqlServer("Server=localhost;Database=trainingdb;Trusted_Connection=True;"));

            services
                .AddGraphQLServer()
                .RegisterDbContext<TrainingDbContext>()
                .AddQueryType<Query>();
            //.AddMutationType<MutationType>()
        }

        public void ConfigureGraphQL(IEndpointRouteBuilder builder) => builder.MapGraphQL();
    }

    public class Query
    {
        // https://chillicream.com/docs/hotchocolate/integrations/entity-framework
        // https://chillicream.com/docs/hotchocolate/v10/schema/descriptor-attributes - v10?!
        // https://chillicream.com/docs/hotchocolate/api-reference/migrate-from-10-to-11
        // https://dev.to/michaelstaib/get-started-with-hot-chocolate-and-entity-framework-e9i
        //[UseSelection] // HotChocolate.Types.Selections
        [UseFiltering]
        [UseSorting]
        public IQueryable<Account> GetAccounts([Service(ServiceKind.Synchronized)] TrainingDbContext ctx) =>
            ctx.Accounts;
    }
}