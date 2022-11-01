using Microsoft.AspNetCore.Mvc;
using ProblemSource.Models;
using TrainingApi.Services;

namespace TrainingApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly OldDbRaw oldDb;
        private readonly ILogger<AggregatesController> _logger;

        public AccountsController(OldDbRaw oldDb, ILogger<AggregatesController> logger)
        {
            this.oldDb = oldDb;
            _logger = logger;
        }

        [HttpPost]
        public async Task<Account> Post(AccountCreateDTO dto)
        {
            return new Account();
        }

        [HttpPut]
        public async Task<Account> Put(AccountCreateDTO dto)
        {
            return new Account();
        }

        public class AccountCreateDTO
        {
            public string TrainingPlan { get; set; } = "";
            public TrainingSettings TrainingSettings { get; set; } = new TrainingSettings();
        }

        [HttpGet]
        public async Task<IEnumerable<Account>> Get(int skip = 0, int take = 0, string? orderBy = null, bool descending = false)
        {
            var query = $@"
SELECT MAX(other_id) as maxDay, MAX(latest_underlying) as latest, account_id
FROM aggregated_data
WHERE aggregator_id = 2
GROUP BY account_id
{(orderBy == null ? "" : "ORDER BY " + orderBy + " " + (descending ? "DESC" : "ASC"))}
OFFSET {skip} ROWS FETCH NEXT {take} ROWS ONLY
";
            var result = await oldDb.Read(query, (reader, columns) => new Account { NumDays = reader.GetInt32(0), Latest = reader.GetDateTime(1), Id = reader.GetInt32(2) });
            return result;
        }

        public class Account
        {
            public int Id { get; set; }
            public int NumDays { get; set; }
            public DateTime Latest { get; set; }
        }
    }
}