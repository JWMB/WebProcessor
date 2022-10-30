using Microsoft.EntityFrameworkCore;
using OldDb.Models;
using TrainingApi.Services;

namespace TrainingApiTests
{
    public class UnitTest1
    {
        [Fact]
        public async Task Test1()
        {
            using var db = new TrainingDbContext(new DbContextOptions<TrainingDbContext>() {  });
            var accountId = 715955;
            var account = await db.Accounts.SingleOrDefaultAsync(o => o.Id == accountId);
            var xx = new RecreateLogFromOldDb(db);
            var phases = await xx.Get(715955);
            var log = RecreateLogFromOldDb.PhasesToLogItemsJson(phases);
            //var s = new OldDb.Startup();


        }
    }
}