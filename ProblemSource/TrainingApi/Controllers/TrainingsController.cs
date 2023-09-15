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

namespace TrainingApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TrainingsController : ControllerBase
    {
        private readonly ITrainingPlanRepository trainingPlanRepository;
        private readonly ITrainingRepository trainingRepository;
        private readonly ITrainingTemplateRepository trainingTemplateRepository;
        private readonly IStatisticsProvider statisticsProvider;
        private readonly IUserRepository userRepository;
        private readonly ICurrentUserProvider userProvider;
        private readonly ITrainingUsernameService trainingUsernameService;
        private readonly IAggregationService aggregationService;
        private readonly IUserGeneratedDataRepositoryProviderFactory dataRepoFactory;

        private readonly ILogger<AggregatesController> log;

        public TrainingsController(ITrainingPlanRepository trainingPlanRepository, ITrainingRepository trainingRepository, IStatisticsProvider statisticsProvider, 
            IUserRepository userRepository, ICurrentUserProvider userProvider, ITrainingUsernameService trainingUsernameService, 
            IAggregationService aggregationService, IUserGeneratedDataRepositoryProviderFactory dataRepoFactory,
            ITrainingTemplateRepository trainingTemplateRepository,
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
            this.trainingTemplateRepository = trainingTemplateRepository;
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
            templates = templates ?? await trainingTemplateRepository.GetAll();
            var template = templates.SingleOrDefault(o => o.Id == dto.BaseTemplateId);
            if (template == null)
                throw new Exception($"Template not found: {dto.BaseTemplateId}");
            dto.TrainingSettings ??= template.Settings;
            // TODO: trainingPlanOverrides is incorrectly serialized, so we can't use the one from the DTO
            dto.TrainingSettings.trainingPlanOverrides = template.Settings.trainingPlanOverrides;
            return await trainingRepository.Add(trainingPlanRepository, trainingUsernameService, dto.TrainingPlan ?? template.TrainingPlanName, dto.TrainingSettings, dto.AgeBracket);
        }

        [HttpGet]
        [Route("CreateTrainingsInfo")]
        public async Task<CreateTrainingsInfoDto> GetCreateTrainingsInfo()
        {
            var currentUser = userProvider.UserOrThrow;

            var trainingsInfo = (await currentUser.Trainings.GetTrainingsInfo(trainingRepository, statisticsProvider)).SelectMany(o => o.Value).ToList();
            var numTrainingsWithMinDaysCompleted = trainingsInfo.Count(o => o.Summary?.TrainedDays >= 5);

            return new CreateTrainingsInfoDto { 
                TrainingsQuota = new CreateTrainingsInfoDto.Quota
                {
                    Created = trainingsInfo.Count,
                    Started = trainingsInfo.Count(o => o.Summary?.TrainedDays > 0),
                    Underway = numTrainingsWithMinDaysCompleted,
                    Limit = currentUser.Role == Roles.Admin ? 1000 : Math.Max(60, numTrainingsWithMinDaysCompleted + 35),
                    Reusable = trainingsInfo.Where(o => o.Training.Created < DateTimeOffset.UtcNow.AddDays(-1) && (o.Summary?.TrainedDays ?? 0) == 0).Select(o => o.Id).ToList()
                }
            };
        }

        [HttpPost]
        [Route("createclass")]
        [ProducesErrorResponseType(typeof(HttpException))]
        public async Task<IEnumerable<string>> PostGroup(TrainingCreateDto dto, string groupName, int numTrainings)
        {
            var user = userProvider.UserOrThrow;

            var createTrainingsInfo = await GetCreateTrainingsInfo();

            // TODO: standard validation
            var maxTrainingsInGroup = createTrainingsInfo.MaxTrainingsInGroup;
            if (numTrainings < 1)
                throw new HttpException($"{nameof(numTrainings)}:{numTrainings} exceeds accepted range", StatusCodes.Status400BadRequest);
            else if (numTrainings > maxTrainingsInGroup)
                throw new HttpException($"{nameof(numTrainings)}:{numTrainings} cannot exceed {maxTrainingsInGroup}", StatusCodes.Status400BadRequest);
            if (string.IsNullOrEmpty(groupName) || groupName.Length > 20) throw new HttpException($"Bad parameter: {nameof(groupName)}", StatusCodes.Status400BadRequest);

            var numTrainingsToGetFromOtherGroups = 0;

            if (user.Role != Roles.Admin)
            {
                var numAvailableWithoutReusing = createTrainingsInfo.TrainingsQuota.Limit - createTrainingsInfo.TrainingsQuota.Created;

                numTrainingsToGetFromOtherGroups = numTrainings - numAvailableWithoutReusing;

                if (numTrainingsToGetFromOtherGroups > 0)
                {
                    if (!dto.ReuseTrainingsNotStarted || createTrainingsInfo.TrainingsQuota.Reusable.Count < numTrainingsToGetFromOtherGroups)
                        throw new HttpException(
                            $"You are allowed max {createTrainingsInfo.TrainingsQuota.Limit} trainings.<br/>" 
                            + $"You already have {createTrainingsInfo.TrainingsQuota.Created}.", StatusCodes.Status400BadRequest);
                }
                //if (dto.ReuseTrainingsNotStarted)
                //{
                //    maxTotalTrainings += createTrainingsInfo.TrainingsQuota.Reusable.Count; //Math.Max(0, createTrainingsInfo.TrainingsQuota.Created - createTrainingsInfo.TrainingsQuota.Started);
                //}
                //if (numTrainings + currentNumTrainings > maxTotalTrainings)
                //{
                //    throw new HttpException($"You are allowed max {maxTotalTrainings} trainings. You have {Math.Max(0, maxTotalTrainings - currentNumTrainings)} left.", StatusCodes.Status400BadRequest);
                //}
            }

            var templates = await trainingTemplateRepository.GetAll();

            // TODO: first move / reset 
            if (numTrainingsToGetFromOtherGroups > 0)
            {
                //var idsToUse = (await user.Trainings.RemoveUnusedFromGroups(numTrainingsToGetFromOtherGroups, groupName, trainingRepository, statisticsProvider)).SelectMany(o => o.Value);
            }

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

        [HttpGet]
        [Route("templates")]
        public async Task<IEnumerable<TrainingTemplateDto>> GetTemplates()
        {
            var templates = await trainingTemplateRepository.GetAll();
            var user = userProvider.UserOrThrow;
            if (user.Role != "Admin")
            {
                // Only provide a the default template when not an admin:
                var preferredTemplate = "template_2023HT";
                templates = templates.Where(o => o.Username == preferredTemplate);
                if (!templates.Any())
                    throw new Exception($"Template missing: {preferredTemplate}");
            }

            return templates.Select(o => new TrainingTemplateDto {
                Id = o.Id,
                Name = o.Username.Replace("template_", ""),
                TrainingPlanName = o.TrainingPlanName,
                Settings = o.Settings ?? TrainingSettings.Default
            });
        }

        [HttpGet]
        [Route("groups")]
        public async Task<Dictionary<string, List<TrainingSummaryDto>>> GetGroups()
        {
            var user = userProvider.UserOrThrow;
            var groupedTrainings = await GetUserGroups(user: user);
            var trainings = groupedTrainings.SelectMany(o => o.Value).DistinctBy(o => o.Id).ToList();

            var summaryDtos = await GetSummaryDtos(trainings);

            var groupToIds = groupedTrainings.ToDictionary(o => o.Key, o => o.Value.Select(t => t.Id).ToList());

            return groupToIds.ToDictionary(
                o => o.Key,
                o => o.Value.Select(id => summaryDtos.FirstOrDefault(o => o.Id == id)).OfType<TrainingSummaryDto>().ToList());
        }

        private async Task<List<TrainingSummaryDto>> GetSummaryDtos(IEnumerable<Training> trainings, IEnumerable<TrainingSummary>? summaries = null)
        {
            summaries ??= (await statisticsProvider.GetTrainingSummaries(trainings.Select(o => o.Id))).OfType<TrainingSummary>();
            var summariesAsDict = summaries.ToDictionary(o => o.Id, o => o);

            return trainings.Select(training => 
                TrainingSummaryDto.Create<TrainingSummaryDto>(training, summariesAsDict.GetValueOrDefault(training.Id, null))
                ).ToList();
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

        [Authorize(Roles = RolesRequirement.Admin)]
        [HttpGet]
        [Route("allsummaries")]
        public async Task<List<TrainingSummaryDto>> GetAllSummaries()
        {
            var trainings = (await trainingRepository.GetAll()).ToList();
            var summaries = await statisticsProvider.GetAllTrainingSummaries();
            return await GetSummaryDtos(trainings, summaries);
        }
        
        [HttpGet]
        [Route("summaries")]
        public async Task<List<TrainingSummaryWithDaysDto>> GetSummaries([FromQuery] string? group = null)
        {
            var trainings = await GetUsersTrainings(group: group);
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
            public bool ReuseTrainingsNotStarted { get; set; }
        }

        public class TrainingTemplateDto
        {
            public string Name { get; set; } = string.Empty;
            public int Id { get; set; }
            public string TrainingPlanName { get; set; } = "";
            public TrainingSettings Settings { get; set; } = new TrainingSettings();
        }

        public class CreateTrainingsInfoDto
        {
            public Quota TrainingsQuota { get; set; } = new();

            public int MaxTrainingsInGroup { get; set; } = 35;

            public class Quota
            {
                public int Limit { get; set; }
                public int Created { get; set; }
                public int Started { get; set; }
                public int Underway { get; set; }
                public List<int> Reusable { get; set; } = new();
            }
        }
    }
}
