using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProblemSource;
using ProblemSource.Models;
using ProblemSource.Models.Aggregates;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Services.Storage;
using TrainingApi.Services;

namespace TrainingApi.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    [ApiController]
    [Route("api/[controller]")]
    public class TrainingsController : ControllerBase
    {
        private readonly ITrainingPlanRepository trainingPlanRepository;
        private readonly ITrainingRepository trainingRepository;
        private readonly IStatisticsProvider statisticsProvider;
        private readonly MnemoJapanese mnemoJapanese;
        private readonly UsernameHashing usernameHashing;

        //private readonly IUserStateRepository userStateRepository;
        private readonly ILogger<AggregatesController> _logger;

        public TrainingsController(ITrainingPlanRepository trainingPlanRepository, ITrainingRepository trainingRepository, IStatisticsProvider statisticsProvider, MnemoJapanese mnemoJapanese, UsernameHashing usernameHashing,
            ILogger<AggregatesController> logger)
        {
            this.trainingPlanRepository = trainingPlanRepository;
            this.trainingRepository = trainingRepository;
            this.statisticsProvider = statisticsProvider;
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

        [HttpGet]
        [Route("{id}")]
        public async Task<Training?> GetById(int id)
        {
            return await trainingRepository.Get(id);
        }

        [HttpGet]
        public async Task<List<Training>> Get()
        {
            return (await trainingRepository.GetAll()).ToList();
        }

        [HttpGet]
        [Route("summaries")]
        public async Task<List<TrainingSummary>> GetSummaries()
        {
            var trainings = (await trainingRepository.GetAll()).ToList();
            var trainingDayTasks = trainings.Select(o => statisticsProvider.GetTrainingDays(o.Id)).ToList();

            var results = await Task.WhenAll(trainingDayTasks);

            var tdDict = results
                .Where(o => o.Any())
                .Where(o => o.First().AccountId > 0)
                .ToDictionary(o => o.First().AccountId, o => o.ToList());

            return tdDict.Select(kv => new TrainingSummary
            {
                Id = kv.Key,
                Uuid = kv.Value.FirstOrDefault()?.AccountUuid ?? "N/A", // TODO: if no days trained, Uuid will be missing - should be part of Training
                Days = kv.Value
            }).ToList();
        }

        public class TrainingSummary
        {
            public int Id { get; set; }
            public string Uuid { get; set; } = string.Empty;
            public List<TrainingDayAccount> Days { get; set; } = new();
        }

        public class TrainingCreateDTO
        {
            public string TrainingPlan { get; set; } = "";
            public TrainingSettings TrainingSettings { get; set; } = new TrainingSettings();
        }
    }
}