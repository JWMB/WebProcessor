using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProblemSource.Models;
using ProblemSource.Models.Aggregates;
using ProblemSource.Services;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Models;
using ProblemSourceModule.Models.Aggregates;
using ProblemSourceModule.Services;
using ProblemSourceModule.Services.Storage;
using TrainingApi.ErrorHandling;
using TrainingApi.Services;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        private readonly ICurrentUserProvider userProvider;
        private readonly ITrainingUsernameService trainingUsernameService;
        private readonly IAggregationService aggregationService;
        private readonly IUserGeneratedDataRepositoryProviderFactory dataRepoFactory;

        //private readonly IUserStateRepository userStateRepository;
        private readonly ILogger<AggregatesController> log;

        public TrainingsController(ITrainingPlanRepository trainingPlanRepository, ITrainingRepository trainingRepository, IStatisticsProvider statisticsProvider, 
            IUserRepository userRepository, ICurrentUserProvider userProvider, ITrainingUsernameService trainingUsernameService, 
            IAggregationService aggregationService, IUserGeneratedDataRepositoryProviderFactory dataRepoFactory,
            ILogger<AggregatesController> logger)
        {
            this.trainingPlanRepository = trainingPlanRepository;
            this.trainingRepository = trainingRepository;
            this.statisticsProvider = statisticsProvider;
            this.userRepository = userRepository;
            this.userProvider = userProvider;
            this.trainingUsernameService = trainingUsernameService;
            this.aggregationService = aggregationService;
            this.dataRepoFactory = dataRepoFactory;
            log = logger;
        }

        [HttpPost]
        public async Task<string> Post(TrainingCreateDto dto)
        {
            var training = await CreateTraining(dto);
            return training.Username;
        }

        [Authorize(Policy = RolesRequirement.Admin)]
        [HttpDelete]
        public async Task Delete(int id, bool deleteTrainingDataOnly = true)
        {
            var training = await trainingRepository.Get(id);
            if (training == null)
                return;

            var fact = dataRepoFactory.Create(id);
            await fact.RemoveAll();

            if (deleteTrainingDataOnly == false)
                await trainingRepository.RemoveByIdIfExists(id);
        }

        private async Task<Training> CreateTraining(TrainingCreateDto dto, IEnumerable<Training>? templates = null)
        {
            templates = templates ?? await GetTrainingTemplates();
            var template = templates.SingleOrDefault(o => o.Id == dto.BaseTemplateId);
            if (template == null)
                throw new Exception($"Template not found: {dto.BaseTemplateId}");

            // TODO: use dto.TrainingSettings ?? template.Settings once response serialization works
            return await trainingRepository.Add(trainingPlanRepository, trainingUsernameService, dto.TrainingPlan ?? template.TrainingPlanName, template.Settings, dto.AgeBracket);
        }

        private async Task<User> GetImpersonatedUserOrThrow(string? username)
        {
            var currentUser = userProvider.UserOrThrow;
            if (string.IsNullOrEmpty(username))
                return currentUser;

            if (currentUser.Role != Roles.Admin)
                throw new UnauthorizedAccessException();

            var impersonatedUser = await userRepository.Get(username);
            if (impersonatedUser == null)
                throw new Exception($"null user '{username}'");

            return impersonatedUser;
        }

        [HttpPost]
        [Route("createclass")]
        [ProducesErrorResponseType(typeof(HttpException))]
        public async Task<IEnumerable<string>> PostGroup(TrainingCreateDto dto, string groupName, int numTrainings, string? createForUser = null)
        {
            var user = userProvider.UserOrThrow;

            // TODO: standard validation
            if (numTrainings < 1 || numTrainings > 30) throw new HttpException($"{nameof(numTrainings)} exceeds accepted range", StatusCodes.Status400BadRequest);
            if (string.IsNullOrEmpty(groupName) || groupName.Length > 20) throw new HttpException($"Bad parameter: {nameof(groupName)}", StatusCodes.Status400BadRequest);

            if (user.Role != Roles.Admin)
            {
                var currentNumTrainings = user.Trainings.Sum(o => o.Value.Count());
                var max = 50;
                if (numTrainings + currentNumTrainings > max)
                {
                    throw new HttpException($"We currently allow max {max} trainings per account. You have {Math.Max(0, max - currentNumTrainings)} left.", StatusCodes.Status400BadRequest);
                }
            }

            if (string.IsNullOrEmpty(createForUser) == false)
                user = await GetImpersonatedUserOrThrow(createForUser);

            var templates = await GetTrainingTemplates();

            var trainings = new List<Training>();
            for (int i = 0; i < numTrainings; i++)
                trainings.Add(await CreateTraining(dto, templates));

            if (!user.Trainings.TryGetValue(groupName, out var list))
            {
                list = new List<int>();
                user.Trainings.Add(groupName, list);
            }
            list.AddRange(trainings.Select(o => o.Id));
            await userRepository.Update(user);

            return trainings.Select(o => o.Username).ToList();
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<Training?> GetById(int id)
        {
            return await trainingRepository.Get(id);
        }

        [HttpGet]
        public async Task<IEnumerable<Training>> Get()
        {
            return await GetUsersTrainings();
        }

        private Task<IEnumerable<Training>> GetTrainingTemplates()
        {
            // TODO: use real storage, move to service
            var templates = new[] {
                new Training { Id = 1, Username = "template_Default training", TrainingPlanName = "2017 HT template Default", Settings = CreateSettings() },
                new Training { Id = 2, Username = "template_Test training", TrainingPlanName = "2023 VT template JonasTest", Settings = CreateSettings(s => 
                {
                    s.Analyzers = new List<string> { nameof(ProblemSourceModule.Services.TrainingAnalyzers.ExperimentalAnalyzer) };
                    s.timeLimits = new List<decimal> { 3 };
                    s.customData = new CustomData { allowMultipleLogins = true };
                }) },
                new Training { Id = 3, Username = "template_NumberlineTest training", TrainingPlanName = "2023 VT template JonasTest", Settings = CreateSettings(s => {
                    s.customData = new CustomData { 
                        allowMultipleLogins = true,
                        numberLine = new { changeSuccess = 0.4, changeFail = -1 } //skillChangeGood = 0.5 }
                    };
                    // TODO: Response serialization doesn't work (probably .NET built-in JSON vs dynamic/JToken)
                    s.UpdateTrainingOverrides(new[]{ TrainingSettings.CreateWeightChangeTrigger(new Dictionary<string, int> { {"Math", 100}, { "numberline", 100 } }, 0, 0) });
                }) },
            };

            return Task.FromResult((IEnumerable<Training>)templates);

            TrainingSettings CreateSettings(Action<TrainingSettings>? act = null)
            {
                var ts = TrainingSettings.Default;
                act?.Invoke(ts);
                return ts;
            }
        }

        [HttpGet]
        [Route("templates")]
        public async Task<IEnumerable<TrainingTemplateDto>> GetTemplates()
        {
            return (await GetTrainingTemplates()).Select(o => new TrainingTemplateDto {
                Id = o.Id,
                Name = o.Username.Replace("template_", ""),
                TrainingPlanName = o.TrainingPlanName,
                Settings = o.Settings ?? TrainingSettings.Default
            });
        }

        [HttpGet]
        [Route("groups")]
        public async Task<Dictionary<string, List<TrainingSummaryDto>>> GetGroups(string? impersonateUser = null) // TODO: use a header instead and centralize impersonation ICurrentUserProvider?
        {
            var user = await GetImpersonatedUserOrThrow(impersonateUser);
            var groupedTrainings = await GetUserGroups(user: user);
            var trainings = groupedTrainings.SelectMany(o => o.Value).DistinctBy(o => o.Id).ToList();

            var summaryDtos = await GetSummaryDtos(trainings);

            var groupToIds = groupedTrainings.ToDictionary(o => o.Key, o => o.Value.Select(t => t.Id).ToList());

            return groupToIds.ToDictionary(
                o => o.Key,
                o => o.Value.Select(id => summaryDtos.FirstOrDefault(o => o.Id == id)).OfType<TrainingSummaryDto>().ToList());
        }

        private async Task<List<TrainingSummaryDto>> GetSummaryDtos(IEnumerable<Training> trainings)
        {
            var summaries = await statisticsProvider.GetTrainingSummaries(trainings.Select(o => o.Id));
            return trainings.Select(training => TrainingSummaryDto.Create<TrainingSummaryDto>(trainings.Single(t => t.Id == training.Id), summaries.FirstOrDefault(s => s?.Id == training.Id))).ToList();
        }

        [HttpPost]
        [Authorize(Policy = RolesRequirement.Admin)]
        [Route("refresh")]
        public async Task<int> RefreshStatistics([FromBody] IEnumerable<int> trainingIds)
        {
            foreach (var id in trainingIds)
            {
                var repo = dataRepoFactory.Create(id);
                await aggregationService.UpdateAggregates(repo, new List<LogItem>(), id);
            }
            return trainingIds.Any() ? trainingIds.FirstOrDefault() : 0;
        }

        [HttpGet]
        [Route("summaries")]
        public async Task<List<TrainingSummaryWithDaysDto>> GetSummaries([FromQuery] string? group = null, string? impersonateUser = null)
        {
            var trainings = await GetUsersTrainings(group: group, user: await GetImpersonatedUserOrThrow(impersonateUser));
            var trainingDayTasks = trainings.Select(o => statisticsProvider.GetTrainingDays(o.Id)).ToList();

            IEnumerable<TrainingDayAccount>[] results;
            try
            {
                results = await Task.WhenAll(trainingDayTasks);
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"group = {group}");
                throw;
            }

            var summaries = await statisticsProvider.GetTrainingSummaries(trainings.Select(o => o.Id));

            var daysById = results
                .Where(o => o.Any())
                .Where(o => o.First().AccountId > 0)
                .ToDictionary(o => o.First().AccountId, o => o.ToList());

            return trainings.Select(training => 
                TrainingSummaryWithDaysDto.Create(training, summaries.FirstOrDefault(o => o?.Id == training.Id), daysById.GetValueOrDefault(training.Id, new List<TrainingDayAccount>()))
            ).ToList();
        }

        private async Task<Dictionary<string, List<Training>>> GetUserGroups(string? group = null, User? user = null)
        {
            user = user ?? userProvider.UserOrThrow;
            Dictionary<string, List<Training>> groupToIds = new();

            if (user.Trainings.Any() == false && user.Role == Roles.Admin)
                groupToIds.Add("", (await trainingRepository.GetAll()).ToList());
            else
            {
                List<int> fetchIds;
                if (group != null && user.Trainings.TryGetValue(group, out var ids))
                    fetchIds = ids;
                else
                    fetchIds = user.Trainings.SelectMany(o => o.Value).Distinct().ToList();

                var trainings = await trainingRepository.GetByIds(fetchIds);
                groupToIds = user.Trainings.ToDictionary(o => o.Key, o => trainings.Where(t => o.Value.Contains(t.Id)).ToList());
            }
            return groupToIds;
        }

        private async Task<IEnumerable<Training>> GetUsersTrainings(string? group = null, User? user = null)
        {
            return (await GetUserGroups(user: user, group: group)).SelectMany(o => o.Value).DistinctBy(o => o.Id);
        }

        public class TrainingSummaryDto
        {
            public int Id { get; set; }
            public string Username { get; set; } = string.Empty;
            public DateTimeOffset Created { get; set; }
            public int TrainedDays { get; set; }
            public int TargetDays { get; set; } = 35;
            public decimal AvgResponseMinutes { get; set; }
            public decimal AvgRemainingMinutes { get; set; }
            public decimal TargetMinutesPerDay { get; set; }
            public decimal AvgAccuracy { get; set; }
            public DateTimeOffset? FirstLogin { get; set; }
            public DateTimeOffset? LastLogin { get; set; }

            public static T Create<T>(Training training, TrainingSummary? summary) where T : TrainingSummaryDto, new()
            {
                return new T
                {
                    Id = training.Id,
                    Username = training.Username,
                    Created = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero), // TODO: training.CreatedAt
                    TrainedDays = summary?.TrainedDays ?? 0,
                    TargetDays = 30, // TODO: training settings
                    AvgResponseMinutes = summary?.AvgResponseMinutes ?? 0,
                    AvgRemainingMinutes = summary?.AvgRemainingMinutes ?? 0,
                    TargetMinutesPerDay = training.Settings?.timeLimits.FirstOrDefault() ?? 33,
                    AvgAccuracy = summary?.AvgAccuracy ?? 0,
                    FirstLogin = summary?.FirstLogin,
                    LastLogin = summary?.LastLogin,
                };
            }
        }

        public class TrainingSummaryWithDaysDto : TrainingSummaryDto
        {
            public List<TrainingDayAccount> Days { get; set; } = new();
            public static TrainingSummaryWithDaysDto Create(Training training, TrainingSummary? summary, IEnumerable<TrainingDayAccount>? days)
            {
                var dto = Create<TrainingSummaryWithDaysDto>(training, summary);
                dto.Days = days?.ToList() ?? new List<TrainingDayAccount>();
                return dto;
            }
        }

        public class TrainingCreateDto
        {
            public int BaseTemplateId { get; set; }
            public string? TrainingPlan { get; set; }
            public TrainingSettings? TrainingSettings { get; set; }
            public string? AgeBracket { get; set; }
        }

        public class TrainingTemplateDto
        {
            public string Name { get; set; } = string.Empty;
            public int Id { get; set; }
            public string TrainingPlanName { get; set; } = "";
            public TrainingSettings Settings { get; set; } = new TrainingSettings();
        }
    }
}
