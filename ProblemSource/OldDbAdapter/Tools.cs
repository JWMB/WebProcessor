using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OldDb.Models;
using Organization;
using ProblemSource.Models;
using ProblemSource.Services;
using ProblemSource.Services.Storage.AzureTables;
using ProblemSourceModule.Models;
using ProblemSourceModule.Services.Storage;
using ProblemSourceModule.Services.Storage.AzureTables;

namespace OldDbAdapter
{
    public class Tools
    {
        private DbSql dbSql = new DbSql("Server=localhost;Database=trainingdb;Trusted_Connection=True;TrustServerCertificate=True");

        private readonly IAggregationService aggregationService;
        private readonly ITypedTableClientFactory tableClientFactory;
        private readonly TrainingDbContext dbContext;
        private readonly AzureTableTrainingRepository trainingRepo;

        public Tools(AzureTableConfig azureTableConfig)
        {
            var loggerFactory = LoggerFactory.Create(c => { });
            aggregationService = new AggregationService(new Logger<AggregationService>(loggerFactory));
            tableClientFactory = new TypedTableClientFactory(azureTableConfig);
            dbContext = new TrainingDbContext();
            trainingRepo = new AzureTableTrainingRepository(tableClientFactory);
        }

        private static string GetSqlForAccountsWithMinNumDays(int minDays, string? additionalWhereClause = null)
        {
            return $@"SELECT [account_id] --, MAX([other_id])
  FROM [aggregated_data] WHERE aggregator_id = 2
{additionalWhereClause}
  GROUP BY account_id
  HAVING MAX([other_id]) >= {minDays}";
        }

        public async Task<Dictionary<string, List<int>>> GetTeachersWithTrainings(int minDays, int minTrainings)
        {
            var q = $@"WITH cte AS (
{GetSqlForAccountsWithMinNumDays(minDays)}
)
SELECT cte.account_id, groups.id as group_id, groups.name FROM cte
INNER JOIN accounts_groups ON accounts_groups.account_id = cte.account_id
INNER JOIN groups ON groups.id = accounts_groups.group_id
WHERE groups.name LIKE 'Teacher %'";

            var rows = await dbSql.Read(q, (reader, cols) => new { account_id = reader.GetInt32(0), group_id = reader.GetInt32(1), name = reader.GetString(2) });

            var byGroupName = rows.GroupBy(o => o.name).Where(o => o.Count() >= minTrainings).ToDictionary(o => o.Key, o => o.Select(p => p.account_id).ToList());

            return byGroupName;
        }

        public async Task MoveTeacherAndTrainingsToAzureTables(int adminId, bool actuallyWrite = false)
        {
            var login = await dbContext.Admins.FirstOrDefaultAsync(o => o.Id == adminId);
            if (login == null)
                throw new NullReferenceException($"Id = {adminId}");
            
            var group = $"Teacher {adminId}";
            
            var renderer = new SqlGroupExpressionRenderer(new SqlGroupExpressionRenderer.M2MTableConfig(), new SqlGroupExpressionRenderer.GroupTableConfig());
            var sqlAccountIds = BooleanExpressionTree.ParseAndRender($"\"{group}\"", renderer);

            var accountIds = await dbSql.Read(GetSqlForAccountsWithMinNumDays(20, $"AND account_id IN ({sqlAccountIds})"), (rd, cols) => rd.GetInt32(0));
            //var aoao = await dbSql.Read($"SELECT * FROM accounts WHERE id IN ({sqlAccountIds})", (rd, cols) => new { A = 1 });

            var q = $@"SELECT account_id, name FROM accounts_groups
  INNER JOIN groups ON groups.id = accounts_groups.group_id
  WHERE account_id IN ({string.Join(",", accountIds)}) AND name LIKE '_class %'";
            var classes = await dbSql.Read(q, (rd, cols) => new { Id = rd.GetInt32(0), Class = rd.GetString(1) });

            var idsByClass = classes.GroupBy(o => o.Class).ToDictionary(o => o.Key.Replace("_class ", ""), o => o.Select(o => o.Id).ToList());

            if (actuallyWrite)
            {
                IUserRepository userRepo = new AzureTableUserRepository(tableClientFactory);
                await userRepo.Upsert(new User { Email = login.Email, Role = "Teacher", Trainings = new UserTrainingsCollection(idsByClass)});
            }

            var allTrainingIds = idsByClass.SelectMany(o => o.Value).ToList();

            Console.WriteLine($"move {allTrainingIds.Count} trainings");
            await MoveToAzureTables(allTrainingIds, actuallyWrite);
        }

        public async Task<List<LogItem>> CreateLogFromOldTraining(int trainingId)
        {
            var logItems = await RecreateLogFromOldDb.GetAsLogItems(dbContext, trainingId);
            return logItems;
        }

        public async Task MoveToAzureTables(IEnumerable<int> trainingIds, bool actuallyWrite = false)
        {
            var accounts = await dbContext.Accounts.Where(o => trainingIds.Contains(o.Id)).ToListAsync();

            foreach (var trainingId in trainingIds)
            {
                Console.WriteLine($"start move training {trainingId}");
                var acc = accounts.FirstOrDefault(o => o.Id == trainingId);
                if (acc == null)
                {
                    continue;
                }

                TrainingSettings? settings = null;
                if (!string.IsNullOrEmpty(acc.TrainingSettings))
                {
                    settings = System.Text.Json.JsonSerializer.Deserialize<TrainingSettings>(acc.TrainingSettings);
                    if (settings == null)
                    {
                    }
                }
                var repos = new AzureTableUserGeneratedDataRepositoryProvider(tableClientFactory, trainingId);
                var logItems = await RecreateLogFromOldDb.GetAsLogItems(dbContext, trainingId);

                if (actuallyWrite)
                {
                    await trainingRepo.Upsert(new Training { Id = trainingId, Settings = settings ?? TrainingSettings.Default, TrainingPlanName = $"Id_{acc.TrainingPlanId}" });
                    await aggregationService.UpdateAggregates(repos, logItems, trainingId);
                }
            }
        }

        public async Task<List<string>> GetRelevantTeachersFromOldDb()
        {
            var byGroupName = await GetTeachersWithTrainings(minDays: 5, minTrainings: 15);
            var orderedByMostTrainings = byGroupName.OrderByDescending(o => o.Value.Count()).ToList();
            //await Z(withMostTrainings.Value.Take(10));
            //var totalGoodTrainingsForTeachers = byGroupName.Values.SelectMany(o => o).Distinct().ToList();

            var teacherIds = orderedByMostTrainings.Select(o => int.Parse(o.Key.Replace("Teacher ", ""))).ToList();
            var teachers = (await dbSql.Read($"SELECT id, email FROM [admins] WHERE id IN ({string.Join(",", teacherIds)})", (reader, cols) =>
                new { id = reader.GetInt32(0), email = reader.GetString(1) }))
                .OrderBy(o => teacherIds.IndexOf(o.id))
                .ToList();
 
            var disallowed = new[] { "gmail", "outlook", "hotmail", "zonline.se", "telia", "robinson.nu", "icloud", "freinet.nu", "childmind.org", "dibber.com", "msn.com", "bigpond.com", "me.com" };
            var selectedTeachers = teachers
                .Select(o => new { Domain = o.email.Substring(o.email.IndexOf("@") + 1).ToLower(), Email = o.email })
                .Where(o => disallowed.Any(d => o.Domain.Contains(d)) == false);

            var uniqueDomains = selectedTeachers.Select(o => o.Domain).Distinct().Order();
            var suspect = uniqueDomains.Where(o => !o.EndsWith(".se") && !o.EndsWith(".fi") && !o.EndsWith(".dk") && !o.EndsWith(".no")).ToList();

            return selectedTeachers
                    .OrderBy(o => o.Domain)
                    .Select(o => o.Email)
                    .ToList();
        }
    }

    //public class RecreatedStatisticsProvider : IStatisticsProvider
    //{
    //    private readonly TrainingDbContext dbContext;

    //    public RecreatedStatisticsProvider(TrainingDbContext dbContext)
    //    {
    //        this.dbContext = dbContext;
    //    }

    //    public async Task<IEnumerable<PhaseStatistics>> GetPhaseStatistics(int accountId) => PhaseStatistics.Create(accountId, await RecreatePhases(accountId));

    //    public async Task<IEnumerable<TrainingDayAccount>> GetTrainingDays(int accountId) => TrainingDayAccount.Create(0, await RecreatePhases(accountId));

    //    private async Task<List<Phase>> RecreatePhases(int accountId)
    //    {
    //        var phases = await RecreateLogFromOldDb.GetFullPhases(dbContext, accountId);
    //        var log = RecreateLogFromOldDb.ToLogItems(phases);
    //        return LogEventsToPhases.Create(log, null).PhasesCreated;
    //    }

    //    public Task<IEnumerable<TrainingSummary?>> GetTrainingSummaries(IEnumerable<int> trainingIds) => throw new NotImplementedException();
    //}

}
