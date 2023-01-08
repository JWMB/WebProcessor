using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProblemSource;
using ProblemSource.Models;
using ProblemSource.Models.Aggregates;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Models.Aggregates;
using ProblemSourceModule.Services.Storage;
using System.Linq;
using System.Security.Claims;
using TrainingApi.Services;
using static TrainingApi.Controllers.TrainingsController;

namespace TrainingApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TrainingsController : ControllerBase
    {
        private readonly ITrainingPlanRepository trainingPlanRepository;
        private readonly ITrainingRepository trainingRepository;
        private readonly IStatisticsProvider statisticsProvider;
        private readonly IUserRepository userRepository;
        private readonly MnemoJapanese mnemoJapanese;
        private readonly UsernameHashing usernameHashing;

        //private readonly IUserStateRepository userStateRepository;
        private readonly ILogger<AggregatesController> _logger;

        public TrainingsController(ITrainingPlanRepository trainingPlanRepository, ITrainingRepository trainingRepository, IStatisticsProvider statisticsProvider, 
            IUserRepository userRepository, MnemoJapanese mnemoJapanese, UsernameHashing usernameHashing,
            ILogger<AggregatesController> logger)
        {
            this.trainingPlanRepository = trainingPlanRepository;
            this.trainingRepository = trainingRepository;
            this.statisticsProvider = statisticsProvider;
            this.userRepository = userRepository;
            this.mnemoJapanese = mnemoJapanese;
            this.usernameHashing = usernameHashing;
            _logger = logger;
        }

        [HttpPost]
        public async Task<string> Post(TrainingCreateDto dto)
        {
            var tp = await trainingPlanRepository.Get(dto.TrainingPlan);
            if (tp == null)
                throw new Exception($"Training plan not found: {dto.TrainingPlan}");
            var id = await trainingRepository.Add(new Training { TrainingPlanName = dto.TrainingPlan });

            return usernameHashing.Hash(mnemoJapanese.FromIntWithRandom(id));
        }

        //[HttpPut]
        //public async Task<string> Put(TrainingCreateDTO dto)
        //{
        //    await Task.Delay(1);
        //    return "";
        //}

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
        [Route("groups")]
        public async Task<Dictionary<string, List<TrainingSummaryDto>>> GetGroups()
        {
            var groupedTrainings = await GetUserGroups();
            var trainings = groupedTrainings.SelectMany(o => o.Value).DistinctBy(o => o.Id).ToList();

            var summaries = await statisticsProvider.GetTrainingSummaries(trainings.Select(o => o.Id));

            var groupToIds = groupedTrainings.ToDictionary(o => o.Key, o => o.Value.Select(t => t.Id).ToList());

            return groupToIds.ToDictionary(
                o => o.Key,
                o => o.Value.Select(id => TrainingSummaryDto.Create(trainings.Single(t => t.Id == id), summaries.FirstOrDefault(s => s?.Id == id))
                ).ToList());
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [Route("refresh")]
        public async Task<int> RefreshStatistics(IEnumerable<int> trainingIds)
        {
            await Task.Delay(100);
            return trainingIds.Any() ? trainingIds.FirstOrDefault() : 0;
        }


        [HttpGet]
        [Route("summaries")]
        public async Task<List<TrainingSummaryWithDaysDto>> GetSummaries()
        {
            var trainings = await GetUsersTrainings();
            var trainingDayTasks = trainings.Select(o => statisticsProvider.GetTrainingDays(o.Id)).ToList();

            var results = await Task.WhenAll(trainingDayTasks);

            var tdDict = results
                .Where(o => o.Any())
                .Where(o => o.First().AccountId > 0)
                .ToDictionary(o => o.First().AccountId, o => o.ToList());

            return tdDict.Select(kv => new TrainingSummaryWithDaysDto
            {
                Id = kv.Key,
                Uuid = kv.Value.FirstOrDefault()?.AccountUuid ?? "N/A", // TODO: if no days trained, Uuid will be missing - should be part of Training
                Days = kv.Value
            }).ToList();
        }

        private async Task<Dictionary<string, List<Training>>> GetUserGroups()
        {
            var nameClaim = User.Claims.First(o => o.Type == ClaimTypes.Name).Value;
            var user = await userRepository.Get(nameClaim);
            Dictionary<string, List<Training>> groupToIds = new();
            if (user == null)
            {
                if (System.Diagnostics.Debugger.IsAttached && nameClaim == "dev")
                    user = new User { Role = Roles.Admin };
                else
                    return groupToIds;
            }

            if (user.Trainings.Any() == false && user.Role == Roles.Admin)
                groupToIds.Add("", (await trainingRepository.GetAll()).ToList());
            else
            {
                var trainings = await trainingRepository.GetByIds(user.Trainings.SelectMany(o => o.Value).Distinct());
                groupToIds = user.Trainings.ToDictionary(o => o.Key, o => trainings.Where(t => o.Value.Contains(t.Id)).ToList());
            }
            return groupToIds;
        }

        private async Task<IEnumerable<Training>> GetUsersTrainings()
        {
            return (await GetUserGroups()).SelectMany(o => o.Value).DistinctBy(o => o.Id);
        }

        public class TrainingSummaryDto
        {
            public int Id { get; set; }
            public string Username { get; set; } = string.Empty;
            public DateTimeOffset Created { get; set; }
            public int TrainedDays { get; set; }
            public decimal AvgResponseMinutes { get; set; }
            public decimal AvgRemainingMinutes { get; set; }
            public decimal AvgAccuracy { get; set; }
            public DateTimeOffset? FirstLogin { get; set; }
            public DateTimeOffset? LastLogin { get; set; }

            public static TrainingSummaryDto Create(Training training, TrainingSummary? summary)
            {
                return new TrainingSummaryDto
                {
                    Id = training.Id,
                    Username = "N/A",
                    Created = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero), // training.CreatedAt
                    TrainedDays = summary?.TrainedDays ?? 0,
                    AvgResponseMinutes = summary?.AvgResponseMinutes ?? 0,
                    AvgRemainingMinutes = summary?.AvgRemainingMinutes ?? 0,
                    AvgAccuracy = summary?.AvgAccuracy ?? 0,
                    FirstLogin = summary?.FirstLogin,
                    LastLogin = summary?.LastLogin,
                };
            }
        }

        //public class GroupTrainingSummary
        //{
        //    public int NumTrainings { get; set; }
        //    public int NumTrainingsStarted { get; set; }
        //    public decimal AverageNumTrainingDays { get; set; }
        //    public DateTimeOffset FirstLogin { get; set; }
        //    public DateTimeOffset LastLogin { get; set; }
        //    public decimal NumTrainingsLoggedInWeekToLastLogin { get; set; }

        //    public static GroupTrainingSummary Create(IEnumerable<TrainingSummary> items)
        //    {
        //        var lastLogin = items.Max(o => o.LastLogin);
        //        var checkFrom = lastLogin.AddDays(-7);
        //        return new GroupTrainingSummary
        //        {
        //            NumTrainings = items.Count(),
        //            NumTrainingsStarted = items.Count(o => o.TrainedDays > 0),
        //            AverageNumTrainingDays = items.Average(o => 1M * o.TrainedDays),
        //            FirstLogin = items.Min(o => o.FirstLogin),
        //            LastLogin = lastLogin,
        //            NumTrainingsLoggedInWeekToLastLogin = items.Count(o => o.LastLogin >= checkFrom)
        //        };
        //    }
        //}

        public class TrainingSummaryWithDaysDto
        {
            public int Id { get; set; }
            public string Uuid { get; set; } = string.Empty;
            public List<TrainingDayAccount> Days { get; set; } = new();
        }

        public class TrainingCreateDto
        {
            public string TrainingPlan { get; set; } = "";
            public TrainingSettings TrainingSettings { get; set; } = new TrainingSettings();
        }
    }
}