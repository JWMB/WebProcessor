using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProblemSource;
using ProblemSource.Models;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Services.Storage;

namespace TrainingApi.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    [ApiController]
    [Route("[controller]")]
    public class TrainingsController : ControllerBase
    {
        public static readonly string XX = "";
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
    }
}