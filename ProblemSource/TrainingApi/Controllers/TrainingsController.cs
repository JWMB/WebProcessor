using Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using ProblemSource.Models;
using ProblemSource.Models.Aggregates;
using ProblemSource.Services;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Models;
using ProblemSourceModule.Models.Aggregates;
using ProblemSourceModule.Services;
using ProblemSourceModule.Services.Storage;
using ProblemSourceModule.Services.TrainingAnalyzers;
using TrainingApi.Authorization;
using TrainingApi.ErrorHandling;
using TrainingApi.Services;

namespace TrainingApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TrainingsController : ControllerBase
    {
        private readonly ITrainingRepository trainingRepository;
        private readonly ITrainingTemplateRepository trainingTemplateRepository;
        private readonly AiCoachAnalyzer aiAnalyzer;
        private readonly ILlmService llmService;
        private readonly ITrainingImporter importer;
        private readonly IMongoDatabase db;
        private readonly IStatisticsProvider statisticsProvider;
        private readonly IUserRepository userRepository;
        private readonly ICurrentUserProvider userProvider;
        private readonly ITrainingUsernameService trainingUsernameService;
        private readonly IAggregationService aggregationService;
        private readonly IUserGeneratedDataRepositoryProviderFactory dataRepoFactory;

        private readonly ILogger<AggregatesController> log;
		private readonly IMongoDatabase? database; // c'mon, stupid mix of abstract interfaces and direct calls!?!

		public TrainingsController(ITrainingPlanRepository trainingPlanRepository, ITrainingRepository trainingRepository, IStatisticsProvider statisticsProvider,
            IUserRepository userRepository, ICurrentUserProvider userProvider, ITrainingUsernameService trainingUsernameService,
            IAggregationService aggregationService, IUserGeneratedDataRepositoryProviderFactory dataRepoFactory,
            ITrainingTemplateRepository trainingTemplateRepository, AiCoachAnalyzer aiAnalyzer, ILlmService llmService, ITrainingImporter importer, IMongoDatabase db,
            ILogger<AggregatesController> logger, IMongoDatabase? database)
        {
            //this.trainingPlanRepository = trainingPlanRepository;
            this.trainingRepository = trainingRepository;
            this.statisticsProvider = statisticsProvider;
            this.userRepository = userRepository;
            this.userProvider = userProvider;
            this.trainingUsernameService = trainingUsernameService;
            this.aggregationService = aggregationService;
            this.dataRepoFactory = dataRepoFactory;
            this.trainingTemplateRepository = trainingTemplateRepository;
            this.aiAnalyzer = aiAnalyzer;
            this.llmService = llmService;
            this.importer = importer;
            this.db = db;
            log = logger;
			this.database = database;
		}

        [HttpPost]
        public async Task<string> Post(TrainingCreateDto dto)
        {
            var selector = await CreateTemplateSelector(dto.BaseTemplateId);
            var training = (await CreateTrainings(1, dto, selector)).Single();
            return training.Username;
        }

        [AtLeastRole(Roless.Admin)]
        //[Authorize(Policy = RolesRequirement.Admin)]
        [HttpDelete("many")]
        public async Task DeleteMany(string ids, bool deleteTrainingDataOnly = true)
        {
            // await fetch("https://localhost:7174/api/Trainings/many?ids=6181", { "credentials": "include", "headers": { "content-type": "application/json" }, "method": "DELETE", "mode": "cors"});
            // await fetch("https://curricullm.net/api/Trainings/many?ids=14,12", { "credentials": "include", "headers": { "content-type": "application/json" }, "method": "DELETE", "mode": "cors"});
            var allUsers = await userRepository.GetAll();
            var usersToUpdate = new List<User>();

            var trainingIds = ids.Split(",").Select(o => int.TryParse(o, out var v) ? (int?)v : null).OfType<int>().ToList();
            foreach (var id in trainingIds)
            {
                Training? training = null;
                try
                {
                    training = await trainingRepository.Get(id);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex.Message}");
                }

                if (deleteTrainingDataOnly == false) // delete fully - remove from teachers
                {
                    var affectedUsers = allUsers.Where(o => o.Trainings.GetAllIds().Contains(id));
                    foreach (var user in affectedUsers)
                    {
                        foreach (var group in user.Trainings)
                            group.Value.Remove(id);
                        usersToUpdate.Add(user);
                    }
                }

                if (training != null)
                {
                    var fact = dataRepoFactory.Create(id);
                    await fact.RemoveAll();
                }

                if (deleteTrainingDataOnly == false)
                    await trainingRepository.RemoveByIdIfExists(id);
            }

            var tmp = usersToUpdate.DistinctBy(o => o.Email);
            foreach (var user in tmp)
                await userRepository.Upsert(user);

        }

		[AtLeastRole(Roless.Admin)]
		//[Authorize(Policy = RolesRequirement.Admin)]
		[HttpDelete]
        public async Task Delete(int id, bool deleteTrainingDataOnly = true)
        {
            await DeleteMany($"{id}", deleteTrainingDataOnly);
        }

        private async Task<Training> GetTemplate(int templateId, IEnumerable<Training>? templates = null)
        {
            templates = templates ?? await trainingTemplateRepository.GetAll();
            var template = templates.SingleOrDefault(o => o.Id == templateId);
            if (template == null)
                throw new Exception($"Template not found: {templateId}");
            return template;
        }

        private void UpdateTrainingObject(TrainingCreateDto dto, Training template, Training training) //, string trainingPlanName)
        {
            training.TrainingPlanName = template.TrainingPlanName;
            training.Settings = dto.TrainingSettings ?? template.Settings ?? TrainingSettings.Default;
            // TODO: trainingPlanOverrides is incorrectly serialized, so we can't use the one from the DTO
            training.Settings.trainingPlanOverrides = template.Settings?.trainingPlanOverrides;
            training.Created = DateTimeOffset.UtcNow;

            training.AgeBracket = dto.AgeBracket ?? "";
        }

        private async Task<List<Training>> CreateTrainings(int count, TrainingCreateDto dto, ITrainingTemplateSelector templateSelector, string? templateDiscriminator = null)
        {
			List<string> discriminators = templateDiscriminator?.Any() == true ? [templateDiscriminator] : [];

			var result = new List<Training>();
            for (int i = 0; i < count; i++)
            {
                var training = new Training();
                var template = templateSelector.SelectOrThrow(new[] { $"{templateDiscriminator}{i}" }.Concat(discriminators));
                UpdateTrainingObject(dto, template, training);
                await trainingRepository.Add(trainingUsernameService, training);
                result.Add(training);
            }
            return result;
        }

        [HttpGet]
        [Route("CreateTrainingsInfo")]
        public async Task<CreateTrainingsInfoDto> GetCreateTrainingsInfo()
        {
            var currentUser = userProvider.UserOrThrow;

            var trainingsInfo = (await currentUser.Trainings.GetTrainingsInfo(trainingRepository, statisticsProvider)).SelectMany(o => o.Value).ToList();
            var numTrainingsWithMinDaysCompleted = trainingsInfo.Count(o => o.Summary?.TrainedDays >= 5);

            return new CreateTrainingsInfoDto
            {
                TrainingsQuota = new CreateTrainingsInfoDto.Quota
                {
                    Created = trainingsInfo.Count,
                    Started = trainingsInfo.Count(o => o.Summary?.TrainedDays > 0),
                    Underway = numTrainingsWithMinDaysCompleted,
                    Limit = (currentUser.Role == Roles.Admin && true) ? 1000 : 30 + Math.Max(60, numTrainingsWithMinDaysCompleted + 35),
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

            var numAvailableWithoutReusing = Math.Max(0, createTrainingsInfo.TrainingsQuota.Limit - createTrainingsInfo.TrainingsQuota.Created);
            var numTrainingsToGetFromOtherGroups = Math.Max(0, numTrainings - numAvailableWithoutReusing);

            if (numTrainingsToGetFromOtherGroups > 0)
            {
                if (!dto.ReuseTrainingsNotStarted || createTrainingsInfo.TrainingsQuota.Reusable.Count < numTrainingsToGetFromOtherGroups)
                    throw new HttpException(
                        $"You are allowed max {createTrainingsInfo.TrainingsQuota.Limit} trainings.<br/>"
                        + $"You already have {createTrainingsInfo.TrainingsQuota.Created}.", StatusCodes.Status400BadRequest);
            }

            var templateSelector = await CreateTemplateSelector(dto.BaseTemplateId);

            var trainings = new List<Training>();

            // TODO: first move / reset 
            if (numTrainingsToGetFromOtherGroups > 0)
            {
                var trainingsToReuse = (await user.Trainings.RemoveUnusedFromGroups(numTrainingsToGetFromOtherGroups, exceptGroup: groupName, trainingRepository, statisticsProvider))
                    .SelectMany(o => o.Value)
                    .ToList();

                if (trainingsToReuse.Count < numTrainingsToGetFromOtherGroups)
                    throw new HttpException("Failed to reuse unused trainings");
                if (trainingsToReuse.Count > numTrainingsToGetFromOtherGroups)
                    throw new HttpException("Failed to reuse unused trainings");

                foreach (var training in trainingsToReuse)
                {
                    var template = templateSelector.SelectOrThrow([groupName, $"{training.Id}"]);
                    UpdateTrainingObject(dto, template, training);
                    await trainingRepository.Update(training);
                }

                trainings.AddRange(trainingsToReuse);
            }

            var numLeftToCreate = numTrainings - numTrainingsToGetFromOtherGroups;
            if (numLeftToCreate > 0)
                trainings.AddRange(await CreateTrainings(numLeftToCreate, dto, templateSelector, groupName));

            await AddTrainingsToUser(user, groupName, trainings);

            return trainings.Select(o => o.Username).ToList();
        }

        private async Task<ITrainingTemplateSelector> CreateTemplateSelector(int? fallbackTemplateId = null, Training? fallbackTemplate = null)
        {
            var templates = await trainingTemplateRepository.GetAll();
            var fallback = fallbackTemplate ?? (fallbackTemplateId != null ? templates.SingleOrDefault(o => o.Id == fallbackTemplateId.Value) : templates.Last());
            return new TrainingTemplateSelector(templates, fallback);
        }

        private async Task AddTrainingsToUser(User user, string groupName, IEnumerable<Training> trainings)
        {
            if (!user.Trainings.TryGetValue(groupName, out var list))
            {
                list = new List<int>();
                user.Trainings.Add(groupName, list);
            }
            var ids = trainings.Select(o => o.Id).Except(list).ToList();
            if (ids.Any())
            {
                list.AddRange(ids);
                await userRepository.Update(user);
            }
        }

        // TODO: use https://learn.microsoft.com/en-us/aspnet/core/web-api/jsonpatch?view=aspnetcore-10.0 - e.g. JsonPatchDocument<Training> 
        [HttpPatch]
        [Route("{id}")]
        public async Task Patch(int id, [FromBody] PatchTrainingDto dto)
        {
            /*
await fetch("https://curricullm.net/api/Trainings/8591", {
    "method": "PATCH", "credentials": "include", "mode": "cors", "headers": { "content-type": "application/json", },
    "body": '{"timeLimit": 2, "allowMultipleLogins": true, "trainingPlanName": "2026 HT Verbal" }',
});
*/
            var user = userProvider.UserOrThrow;
            if (!user.Role.Contains("Admin"))
            {
				if (!user.Trainings.GetAllIds().Contains(id))
					throw new ArgumentOutOfRangeException("Not belonging to user");
			}
			var training = await trainingRepository.Get(id);
            if (training == null)
                throw new ArgumentOutOfRangeException();
            dto.Apply(training);
            await trainingRepository.Upsert(training);
        }

        public class PatchTrainingDto
        {
            public string? Gender { get; set; }
            public string? AgeBracket { get; set; }
            public DateTime? Consent { get; set; }
            public Training.DateInfo? BirthDate { get; set; }

            public bool? AllowMultipleLogins { get; set; }
            public decimal? TimeLimit { get; set; }
            public string? TrainingPlanName { get; set; }

            public void Apply(Training training)
            {
                if (Gender != null)
                    training.Gender = Gender;
                if (AgeBracket != null)
                    training.AgeBracket = AgeBracket;
                if (Consent != null)
                    training.Consent = Consent;
                if (BirthDate != null)
                    training.BirthDate = BirthDate;
                if (AllowMultipleLogins != null)
                {
                    training.Settings ??= new TrainingSettings();
                    training.Settings.customData ??= new CustomData();
                    training.Settings.customData.allowMultipleLogins = AllowMultipleLogins;
                }
                if (TimeLimit != null)
                {
                    training.Settings ??= new TrainingSettings();
                    training.Settings.timeLimits = [TimeLimit.Value];
                }
                if (TrainingPlanName != null)
                    training.TrainingPlanName = TrainingPlanName;
            }
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
        public async Task<IEnumerable<TrainingTemplateDto>> GetTemplates(bool returnOnlyDefaultTemplate = true)
        {
            var templates = await trainingTemplateRepository.GetAll();
            var user = userProvider.UserOrThrow;

            // var returnOnlyDefaultTemplate = user.Role == "Admin" // Only provide a the default template when not an admin:
            if (returnOnlyDefaultTemplate == true)
            {
                var preferredTemplate = "template_2024VT"; // template_2023HT
                templates = templates.Where(o => o.Username == preferredTemplate);
                if (!templates.Any())
                    throw new Exception($"Template missing: {preferredTemplate}");
            }

            return templates.Select(o => new TrainingTemplateDto
            {
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
            // TODO: multiple entries for single day - GroupBy etc shouldn't be necessary
            var summariesAsDict = summaries.GroupBy(o => o.Id)
                .ToDictionary(
                    o => o.Key,
                    o => o.MaxBy(p => p.TrainedDays)
                    );

            return summariesAsDict?.Any() != true
                ? new()
                : trainings.Select(training => new { Training = training, Summary = summariesAsDict!.GetValueOrDefault(training.Id, null) })
                    .Where(o => o.Summary != null)
                    .Select(o => TrainingSummaryDto.Create<TrainingSummaryDto>(o.Training, o.Summary!))
                    .ToList();
        }

        [HttpPost]
		[AtLeastRole(Roless.Admin)]
		//[Authorize(Policy = RolesRequirement.Admin)]
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

		[HttpGet("allphasesstats")]
		[AtLeastRole(Roless.Admin)]
		public async Task<List<object>> GetSomePhaseStats()
		{
            //IMongoDatabase db = client.GetDatabase("");
            if (database == null)
                return [];
            var collection = database.GetCollection<ProblemSourceModule.Services.Storage.MongoDb.MongoTrainingAssociatedDocumentWrapper<PhaseStatistics>>("PhaseStatistics");
            var items = await (await collection.FindAsync(o => true)).ToListAsync();

            var grouped = items.GroupBy(o => o.TrainingId).ToDictionary(
                tr => tr.Key,
                tr => tr.GroupBy(g => g.Document.training_day).ToDictionary(
                    td => td.Key,
                    td => td.GroupBy(ph => $"{ph.Document.exercise}_{ph.Document.timestamp}").Select(
                         ph => ph.OrderByDescending(o => o.Document.end_timestamp).ThenByDescending(o => o.Document.num_questions).First())
                        .GroupBy(o => o.Document.exercise)
                            .Select(lst => new PhaseStatistics {
                                account_id = tr.Key,
                                training_day = td.Key,
                                exercise = lst.Key,

                                level_min = lst.Min(o => o.Document.level_min),
                                level_max = lst.Max(o => o.Document.level_max),
                                num_questions = lst.Sum(o => o.Document.num_questions),
                                num_correct_answers = lst.Sum(o => o.Document.num_correct_answers),
                                response_time_total = lst.Sum(o => o.Document.response_time_total),
                                timestamp = lst.Min(o => o.Document.timestamp),
                                end_timestamp = lst.Min(o => o.Document.timestamp).AddMinutes(lst.Select(o => (o.Document.end_timestamp - o.Document.timestamp).TotalMinutes).Sum())
							})
                            )
                );

            var flat = grouped.Values.SelectMany(o => o.Values.SelectMany(p => p));
            var byDayAndExercise = flat.GroupBy(o => o.training_day).ToDictionary(
                    td => td.Key,
                    td => td.GroupBy(o => o.exercise).ToDictionary(
                        ex => ex.Key,
                        lst => new //PhaseStatistics
                        {
                            account_id = lst.Count(),
                            training_day = td.Key,
							exercise = lst.Key,

							level_min = lst.Min(o => o.level_min),
                            level_max = lst.Max(o => o.level_max),
                            num_questions = lst.Sum(o => (long)o.num_questions),
                            num_correct_answers = lst.Sum(o => (long)o.num_correct_answers),
                            response_time_total = lst.Sum(o => (long)o.response_time_total),
                        })
                    );

            var flat2 = byDayAndExercise.SelectMany(o => o.Value.Select(p => p.Value))
                .OrderBy(o => o.training_day).ThenBy(o => o.exercise).ToList();
            return flat2.Cast<object>().ToList();
		}

		//[Authorize(Roles = RolesRequirement.Admin)]
		[AtLeastRole(Roless.Admin)]
		//[Authorize(Roles = RolesRequirement.SuperAdmin)]
		[HttpGet]
        [Route("allsummaries")]
        public async Task<List<TrainingSummaryDto>> GetAllSummaries()
        {
            var trainings = (await trainingRepository.GetAll()).ToList();
            var summaries = await statisticsProvider.GetAllTrainingSummaries();
            return await GetSummaryDtos(trainings, summaries);
        }

        [HttpGet("trialdata")]
		//[Produces("text/csv")]
		public async Task<IActionResult> GetTrialData(string? trainingIds = null, int? maxRows = null)
        {
            if (database == null)
                return File([], "text/csv");

			var user = userProvider.UserOrThrow;

            var globalPermission = user.Role == Roles.Admin || user.Role == Roles.SuperAdmin;
			List<int>? idList = null;
            if (trainingIds?.Any() == true)
            {
                idList = trainingIds.Split(",").Select(o => o.Trim()).Where(o => o.Any()).Select(int.Parse).Distinct().ToList();

                if (!globalPermission)
                {
					var intersect = user.Trainings.GetAllIds().Intersect(idList);
					if (intersect.Count() < idList.Count)
						throw new ArgumentException("Trainings now owned!");
					idList = intersect.ToList();
				}
			}
            else if (!globalPermission)
                idList = user.Trainings.GetAllIds().ToList();

			var collection = database.GetCollection<ProblemSourceModule.Services.Storage.MongoDb.MongoTrainingAssociatedDocumentWrapper<Phase>>("Phase");

            var all = idList == null ? await collection.FindAsync(o => true) : await collection.FindAsync(o => idList.Contains(o.TrainingId));

            var cnt = 0;
            var byTrainingDayAndPhase = new Dictionary<int, Dictionary<int, Dictionary<string, Phase>>>();
            while (await all.MoveNextAsync())
            {
                if (maxRows.HasValue && cnt > maxRows.Value)
                    break;
                
                var rows = all.Current.ToList();
                foreach (var row in rows)
                {
                    if (!byTrainingDayAndPhase.TryGetValue(row.TrainingId, out var byDay))
                    {
                        byDay = new();
                        byTrainingDayAndPhase[row.TrainingId] = byDay;
					}
                    if (!byDay.TryGetValue(row.Document.training_day, out var byPhase))
                    {
                        byPhase = new();
                        byDay[row.Document.training_day] = byPhase;
					}
                    var phaseId = Phase.UniqueIdWithinUser(row.Document);

					if (maxRows.HasValue && cnt++ > maxRows.Value)
						break;

					if (!byPhase.TryGetValue(phaseId, out var existing))
                        byPhase[phaseId] = row.Document;
                    else if (row.Document.problems.Count >= existing.problems.Count && row.Document.problems.LastOrDefault()?.answers.Count > existing.problems.LastOrDefault()?.answers.Count)
                        byPhase[phaseId] = row.Document;
                    else
                        Console.WriteLine($"lesser {row.TrainingId} {phaseId}");
                }
            }

            var exportRows = byTrainingDayAndPhase.SelectMany(training =>
                training.Value.SelectMany(day =>
                    day.Value.SelectMany(phase =>
                        phase.Value.problems.Select(pb =>
                            new TrialDataExportRow(training.Key, day.Key, phase.Value.exercise, pb.answers.LastOrDefault()?.correct == true,
                            phase.Value.time, pb.problem_string, Math.Round(pb.level, 3), "tp", 0L, ((int?)pb.answers.LastOrDefault()?.time) ?? 0, pb.answers.Count)))))
                .ToList();

            var str = TablularDataHelpers.WriteToString(exportRows);

			//Response.AddHeader("Content-Disposition", "inline; filename=test.pdf");
			return File(System.Text.Encoding.UTF8.GetBytes(str), "text/csv");
		}

		public record TrialDataExportRow(
            int account_id,
            int training_day,
            string exercise,
            bool correct,
            long problem_time,
            string problem_string,
            decimal level,
            string training_plan_name,
            long targetTime,
            int response_time,
            int tries
            // bool is_intro
            // numberline_mod
            // weight_mod
            // age_lower
            // predicted_level
            // predicted_tier
            );

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

            daysById = daysById.ToDictionary(o => o.Key, o =>
            { // TODO: because multiple entries in DB
                return o.Value.GroupBy(o => o.TrainingDay)
                .Select(bd =>
                {
                    return bd.MaxBy(x => x.NumQuestions) ?? bd.MaxBy(x => x.EndTimeStamp) ?? bd.First();
                }).ToList();
            });

            return trainings.Select(training =>
                TrainingSummaryWithDaysDto.Create(training, summaries.FirstOrDefault(o => o?.Id == training.Id), daysById.GetValueOrDefault(training.Id, new List<TrainingDayAccount>()))
            ).ToList();
        }

        [HttpGet]
        [Route("analysis")]
        public async Task<AnalysisDto> GetAiAnalysis(int trainingId, string templateSource, bool onlyPrompt)
        {
            var currentUser = userProvider.UserOrThrow;
            if (!currentUser.Trainings.GetAllIds().Contains(trainingId))
                throw new UnauthorizedAccessException();

            var training = await trainingRepository.Get(trainingId);
            var replacements = await aiAnalyzer.CreateReplacements(trainingId);
            var template = await aiAnalyzer.GetResource(new Uri(templateSource));
            var prompt = await aiAnalyzer.CreatePrompt(template, replacements);
            var completion = "N/A";
            if (onlyPrompt == false)
            {
                try
                {
                    var result = await llmService.Invoke(prompt);
                    completion = result?.Completion ?? "<No completion>";
                }
                catch (Exception ex)
                {
                    completion = $"{ex.GetType().Name}: {ex.Message}";
                }
            }
            return new(prompt, completion);
        }

        [HttpGet]
        [Route("convertid")]
        public string GetConvertId(string id)
        {
            if (int.TryParse(id, out var v))
                return trainingUsernameService.FromId(v);
            if (trainingUsernameService is MnemoJapaneseTrainingUsernameService mjt)
                return $"{mjt.ToId(id)}";
            return "N/A";
        }

		[AtLeastRole(Roless.Admin)]
		//[Authorize(Policy = RolesRequirement.Admin)]
		[HttpGet]
        [Route("alltrainings")]
        public async Task<Dictionary<string, Dictionary<string, List<TrainingSummaryDto>>>> GetAllTrainings(bool onlyStarted = true)
        {
            /*
(await fetch("https://curricullm.net/api/Trainings/alltrainings", {
    "credentials": "include", "method": "GET", "mode": "cors",
    "headers": { "Accept": "application/json" }
})).json()
            */
            var users = db.GetCollection<ProblemSourceModule.Services.Storage.MongoDb.MongoDocumentWrapper<User>>(nameof(User)).Find(o => true).Project(o => new { o.Document.Email, o.Document.Trainings }).ToList();

            var trainingSummaries = db.GetCollection<ProblemSourceModule.Services.Storage.MongoDb.MongoTrainingAssociatedDocumentWrapper<TrainingSummary>>(nameof(TrainingSummary))
                .Find(o => true)
                .Project(o => new { Id = o.TrainingId, o.Document.FirstLogin, o.Document.LastLogin, o.Document.TrainedDays })
                .ToList()
                .GroupBy(o => o.Id)
                .ToDictionary(o =>
                    o.Key,
                    o =>
                    {
                        return o.OrderByDescending(o => o.TrainedDays).ThenByDescending(o => o.LastLogin).First();
                    });
            var trainings = db.GetCollection<ProblemSourceModule.Services.Storage.MongoDb.MongoDocumentWrapper<Training>>(nameof(Training)).Find(o => true)
                .Project(o => new { o.Document.Id, o.Document.Username, o.Document.TrainingPlanName }).ToList();

            var grouped = trainings.GroupBy(o => o.Id);
            var dups = grouped.Where(o => o.Count() > 1).ToList();
            if (dups.Any())
            {
                log.LogWarning($"dups: {string.Join(", ", dups.Select(o => $"{o.Key}:{o.Count()}"))}");
            }

            var byId = trainings.GroupBy(o => o.Id).ToDictionary(grp => grp.Key,
                grp =>
                {
                    var ts = trainingSummaries.TryGetValue(grp.Key, out var summary) ? summary : null;
                    return new TrainingSummaryDto
                    {
                        Id = grp.Key,
                        Username = grp.First().Username,
                        TrainedDays = ts?.TrainedDays ?? 0,
                        FirstLogin = ts?.FirstLogin ?? null,
                        LastLogin = ts?.LastLogin ?? null,
                    };
                });

            var result = users.ToDictionary(
                u => u.Email,
                u => u.Trainings == null ? new() : u.Trainings.ToDictionary(g => g.Key,
                        g => g.Value.Select(o => byId.TryGetValue(o, out var ts) ? ts : new TrainingSummaryDto { Id = o }).ToList()
                    )
                );

            if (onlyStarted)
            {
                result = result.Select(userGroup => new
                {
                    userGroup.Key,
                    Value = userGroup.Value.Select(classGroup => new
                    {
                        classGroup.Key,
                        Value = classGroup.Value.Where(q => q.FirstLogin != null).ToList()
                    })
                    .Where(classGroup => classGroup.Value.Any()).ToDictionary(classGroup => classGroup.Key, classGroup => classGroup.Value)
                })
                .Where(userGroup => userGroup.Value.Any()).ToDictionary(o => o.Key, o => o.Value);
            }

            return result;
        }

        public record AnalysisDto(string Prompt, string Completion);


        [HttpPost("import")]
        public async Task ImportTraining([FromBody] TrainingExport exportDto) //int? targetId = null
        {
            await importer.Import(exportDto); //targetId
        }
        [HttpPost("importmany/{groupName}")]
        public async Task ImportTrainings([FromBody] List<TrainingExport> exports, string groupName)
        {
            /*
await fetch("/api/Trainings/importmany/Norms", {
    "headers": {"Accept": "application/json", "Content-Type": "application/json"},
    "method": "POST", "mode": "cors", "credentials": "include", "body": JSON.stringify(data)
});
             */
            var user = userProvider.UserOrThrow;

            if (exports.Any(o => o.Training == null))
                throw new Exception($"contains null trainings");

            foreach (var item in exports)
            {
                await importer.Import(item);
            }

            await AddTrainingsToUser(user, groupName, exports.Select(o => o.Training!));
        }

        [HttpPatch("randomize")]
        public async Task<ActionResult<List<Training>>> RandomizeGroup(string groupName, [FromBody] List<PatchTrainingDto> variants, string? userId = null)
        {
            // await fetch(`https://curricullm.net/api/Trainings/randomize/${encodeURIComponent("År 1 Carina")}`, { "credentials": "include", "headers": { "content-type": "application/json" }, "method": "PATCH", "mode": "cors", "body": '[{ "trainingPlanName": "2026 HT Math" }, { "trainingPlanName": "2026 HT Verbal" }]' });
            // await fetch(`https://localhost:7174/api/Trainings/randomize/${encodeURIComponent("Fsk A")}`, { "credentials": "include", "headers": { "content-type": "application/json" }, "method": "PATCH", "mode": "cors", "body": '[{ "trainingPlanName": "2026 HT Math" }, { "trainingPlanName": "2026 HT Verbal" }]' });
            // await fetch(`https://localhost:7174/api/Trainings/randomize?userId=${""}&groupName=${encodeURIComponent("Fsk A")}`, { "credentials": "include", "headers": { "content-type": "application/json" }, "method": "PATCH", "mode": "cors", "body": '[{ "trainingPlanName": "2026 HT Math" }, { "trainingPlanName": "2026 HT Verbal" }]' });

            var user = userProvider.UserOrThrow;
            if (userId != user.Email && Enum.Parse<Roless>(user.Role) < Roless.Admin)
                return Forbid();

			var targetUser = await userRepository.Get(userId ?? user.Email);
            if (targetUser == null)
				return BadRequest("User not found");

			if (variants?.Any() != true)
                return BadRequest("Variants not provided");

            if (!targetUser.Trainings.TryGetValue(groupName, out var trainingIds))
                return BadRequest("Group not found");

            var trainingPlanNames = variants.Select(o => o.TrainingPlanName).OfType<string>().Distinct().ToList();
            if (trainingPlanNames.Any())
            {
				var trainingPlans = await trainingTemplateRepository.GetAll();
                var joined = trainingPlanNames.Join(trainingPlans, n => n, tp => tp.TrainingPlanName, (n, tp) => new { n, tp });
                if (joined.Count() < trainingPlanNames.Count)
					return BadRequest("Training plan(s) not found");
			}

			var trainings = await trainingRepository.GetByIds(trainingIds);
            foreach (var training in trainings)
            {
                var variant = variants[(training.Username.GetHashCode() % variants.Count + variants.Count) % variants.Count];
                variant.Apply(training);
                await trainingRepository.Update(training);
			}

            return Ok(trainings);
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

            public string? Gender { get; set; }
            public DateTime? Consent { get; set; }
            public Training.DateInfo? BirthDate { get; set; }

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

                    Gender = training.Gender,
                    Consent = training.Consent,
                    BirthDate = training.BirthDate,
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

	public interface ITrainingTemplateSelector
	{
		Training? Select(IEnumerable<string> discriminators);
		public Training SelectOrThrow(IEnumerable<string> discriminators)
		{
			var selected = Select(discriminators);
			if (selected == null)
				throw new Exception($"No template found: {string.Join(",", discriminators)}");
			return selected;
		}
	}

	public class TrainingTemplateSelector : ITrainingTemplateSelector
	{
		private readonly IEnumerable<Training> templates;
		private readonly Training? fallback;

		public TrainingTemplateSelector(IEnumerable<Training> templates, Training? fallback)
		{
			this.templates = templates;
			this.fallback = fallback;
		}

		//            //var tp = await trainingPlanRepository.Get(template.TrainingPlanName); // trainingPlanName
		//if (tp == null)
		//    throw new Exception($"Training plan not found: {template.TrainingPlanName}");


		public Training? Select(IEnumerable<string> discriminators)
		{
			var template = templates.Where(o => discriminators.Any(p => p.Equals(o.TrainingPlanName, StringComparison.OrdinalIgnoreCase))).FirstOrDefault();
			if (template == null)
			{
				var trainingPlans = new[] {
					//"2026 HT Test Math",
					//"2026 HT Test Verbal"
                    "2026 HT Verbal",
					"2026 HT Math"
				};
				var hashForRandomizedPlan = $"{discriminators.First()}".GetHashCode(); // {groupName}
				hashForRandomizedPlan = hashForRandomizedPlan < 0 ? -hashForRandomizedPlan : hashForRandomizedPlan;
				var templateName = trainingPlans[hashForRandomizedPlan % trainingPlans.Length];
				template = templates.SingleOrDefault(o => o.TrainingPlanName.Equals(templateName, StringComparison.OrdinalIgnoreCase));
			}
			return template ?? fallback;
		}
	}
}
