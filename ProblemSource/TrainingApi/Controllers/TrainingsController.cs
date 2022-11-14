using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProblemSource;
using ProblemSource.Models;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Services.Storage;

namespace TrainingApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class TrainingsController : ControllerBase
    {
        private readonly ITrainingPlanRepository trainingPlanRepository;
        private readonly ITrainingRepository trainingRepository;
        private readonly MnemoJapanese mnemoJapanese;
        private readonly UsernameHashing usernameHashing;

        //private readonly IUserStateRepository userStateRepository;
        private readonly ILogger<AggregatesController> _logger;

        public TrainingsController(ITrainingPlanRepository trainingPlanRepository, ITrainingRepository trainingRepository, MnemoJapanese mnemoJapanese, UsernameHashing usernameHashing,
            ILogger<AggregatesController> logger)
        {
            this.trainingPlanRepository = trainingPlanRepository;
            this.trainingRepository = trainingRepository;
            this.mnemoJapanese = mnemoJapanese;
            this.usernameHashing = usernameHashing;
            _logger = logger;
        }

        [HttpPost]
        public async Task<string> Post(TrainingCreateDTO dto)
        {
            var tp = await trainingPlanRepository.Get(dto.TrainingPlan);
            if (tp == null)
                throw new Exception($"Training plan not found: {dto.TrainingPlan}");
            var id = await trainingRepository.Add(new Training { TrainingPlanName = dto.TrainingPlan });

            return usernameHashing.Hash(mnemoJapanese.FromIntWithRandom(id));
        }

        [HttpPut]
        public async Task<string> Put(TrainingCreateDTO dto)
        {
            await Task.Delay(1);
            return "";
        }


        public class TrainingCreateDTO
        {
            public string TrainingPlan { get; set; } = "";
            public TrainingSettings TrainingSettings { get; set; } = new TrainingSettings();
        }

//        [HttpGet]
//        public async Task<IEnumerable<Account>> Get(int skip = 0, int take = 0, string? orderBy = null, bool descending = false)
//        {
//            var query = $@"
//SELECT MAX(other_id) as maxDay, MAX(latest_underlying) as latest, account_id
//FROM aggregated_data
//WHERE aggregator_id = 2
//GROUP BY account_id
//{(orderBy == null ? "" : "ORDER BY " + orderBy + " " + (descending ? "DESC" : "ASC"))}
//OFFSET {skip} ROWS FETCH NEXT {take} ROWS ONLY
//";
//            var result = await oldDb.Read(query, (reader, columns) => new Account { NumDays = reader.GetInt32(0), Latest = reader.GetDateTime(1), Id = reader.GetInt32(2) });
//            return result;
//        }

//        public class Account
//        {
//            public int Id { get; set; }
//            public int NumDays { get; set; }
//            public DateTime Latest { get; set; }
//        }
    }
}