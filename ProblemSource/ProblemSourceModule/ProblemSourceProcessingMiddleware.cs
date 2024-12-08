using Common.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PluginModuleBase;
using ProblemSource.Models;
using ProblemSource.Models.LogItems;
using ProblemSource.Services;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Models;
using ProblemSourceModule.Services;
using ProblemSourceModule.Services.Storage;
using System.Security.Claims;

namespace ProblemSource
{
    public class ProblemSourceProcessingMiddleware : IProcessingMiddleware
    {
        private readonly ITrainingPlanRepository trainingPlanRepository;
        private readonly IClientSessionManager sessionManager;
        private readonly IDataSink dataSink;
        private readonly IEventDispatcher eventDispatcher;
        private readonly IAggregationService aggregationService;
        private readonly IUserGeneratedDataRepositoryProviderFactory userGeneratedRepositoriesFactory;
        private readonly UsernameHashing usernameHashing;
        private readonly MnemoJapanese mnemoJapanese;
        private readonly ITrainingRepository trainingRepository;
        private readonly TrainingAnalyzerCollection trainingAnalyzers;
        private readonly ILogger<ProblemSourceProcessingMiddleware> log;

        //public bool SupportsMiddlewarePattern => throw new NotImplementedException();

        public ProblemSourceProcessingMiddleware(ITrainingPlanRepository trainingPlanRepository,
            IClientSessionManager sessionManager, IDataSink dataSink, IEventDispatcher eventDispatcher, IAggregationService aggregationService,
            IUserGeneratedDataRepositoryProviderFactory userGeneratedRepositoriesFactory, UsernameHashing usernameHashing, MnemoJapanese mnemoJapanese,
            ITrainingRepository trainingRepository, TrainingAnalyzerCollection trainingAnalyzers,
            ILogger<ProblemSourceProcessingMiddleware> log)
        {
            this.trainingPlanRepository = trainingPlanRepository;
            this.sessionManager = sessionManager;
            this.dataSink = dataSink;
            this.eventDispatcher = eventDispatcher;
            this.aggregationService = aggregationService;
            this.userGeneratedRepositoriesFactory = userGeneratedRepositoriesFactory;
            this.usernameHashing = usernameHashing;
            this.mnemoJapanese = mnemoJapanese;
            this.trainingRepository = trainingRepository;
            this.trainingAnalyzers = trainingAnalyzers;
            this.log = log;
        }

        public async Task Invoke(HttpContext context, RequestDelegate next)
        {
            object result;
            var action = context.Request.RouteValues["action"]?.ToString() ?? "";
            switch (action.ToLower())
            {
                case "deletedata":
                    var uuid = context.Request.Query["uuid"].FirstOrDefault() ?? "";
                    if (!TryGetTrainingIdFromUsername(uuid, false, out var trainingId))
                        result = new SyncResult { error = $"Username not found ({uuid})" };
                    else
                    {
                        var training = await GetTrainingOrThrow(trainingId, context.User);

                        var userRepositories = AssertSession(training, null, null);
                        await userRepositories.RemoveAll();
                        result = new SyncResult { };
                    }
                    break;

                case "syncunauthorized":
                case "sync":
                case "ping":
                    SyncInput root;
                    try
                    {
                        root = await ParseBodyOrThrow(context.Request);
                    }
                    catch (Microsoft.AspNetCore.Connections.ConnectionResetException ex) when (ex.Message.Contains("The client has disconnected"))
                    {
                        return;
                    }

                    var isValidationOnly = root.SessionToken == "validate";
                    if (!TryGetTrainingIdFromUsername(root.Uuid, isValidationOnly, out var trainingId2)) // TODO: co-opting SessionToken for now
                    {
                        result = new SyncResult { error = $"Username not found ({root.Uuid})" };
                    }
                    else
                    {
                        if (action.ToLower() == "ping")
                        {
                            var training = await GetTrainingOrThrow(trainingId2, context.User);
                            await DispatchIncoming(training, root);
                            result = new SyncResult { };
                        }
                        else
                        {
                            result = isValidationOnly
                                ? new SyncResult { messages = $"redirect:/index2.html?autologin={root.Uuid}" }
                                : await Sync(root, context.User);
                        }
                    }
                    break;

                default:
                    result = new SyncResult { error = $"Unknown action: '{action}'" };
                    break;
            }

            await next.Invoke(context);

            await context.Response.WriteAsJsonAsync(result);
        }

        public async Task<SyncResult> Sync(SyncInput root, ClaimsPrincipal user)
        {
            if (!TryGetTrainingIdFromUsername(root.Uuid, false, out var trainingId2)) // TODO: co-opting SessionToken for now
            {
                return new SyncResult { error = $"Username not found ({root.Uuid})" };
            }
            else
            {
                var training = await GetTrainingOrThrow(trainingId2, user); // context.User);
                var syncResult = await Sync(training, root);
                if (syncResult.error != null)
                {
                    log.LogWarning($"Training id={trainingId2} (user='{root.Uuid}') login: {syncResult.error}");
                }
                return syncResult;
            }
        }

        private async Task<Training> GetTrainingOrThrow(int id, ClaimsPrincipal user)
        {
            if (user == null) // For actual sync, we require an authenticated user
                throw new Exception("Unauthenticated"); // TODO: some HttpException with status code

            var training = await trainingRepository.Get(id);
            if (training == null)
                throw new Exception($"Username not found ({id})");

            return training;
        }

        private async Task<SyncInput> ParseBodyOrThrow(HttpRequest request)
        {
            SyncInput? root;
            if (request.Headers.ContentType.Any(o => o?.ToLower().Contains("application/json") == true))
            {
                root = await request.ReadFromJsonAsync<SyncInput>();
            }
            else
            {
                var body = await request.ReadBodyAsStringAsync();
                if (string.IsNullOrEmpty(body))
                    throw new ArgumentNullException($"Request.Body");
                root = System.Text.Json.JsonSerializer.Deserialize<SyncInput?>(body);
            }

            if (root == null)
                throw new ArgumentException("input: incorrect format");

            return root;
        }

        private bool TryGetTrainingIdFromUsername(string uuid, bool validateOnly, out int trainingId)
        {
            // TODO: use regular asp.net model validation
            trainingId = -1;

            // TODO: use regular model validation
            if (string.IsNullOrEmpty(uuid))
                return false;

            // Handle common user input mistakes:
            uuid = uuid.Trim().Replace("  ", " ");

            // TODO: client has already dehashed (but should not, let server handle ui)
            var dehashedUuid = uuid.Contains(" ") ? usernameHashing.Dehash(uuid) : uuid;

            if (dehashedUuid == null)
            {
                if (validateOnly)
                {
                    if (!uuid.Contains(" ") && uuid.Length > 4) // allow for forgotten space
                        dehashedUuid = usernameHashing.Dehash(uuid.Insert(4, " "));

                    if (dehashedUuid == null)
                        return false;
                }
                else
                    return false;
            }

            var id = mnemoJapanese.ToIntWithRandom(dehashedUuid);
            if (id == null)
                return false;

            trainingId = id.Value;

            return true;
        }

        private IUserGeneratedDataRepositoryProvider AssertSession(Training training, string? sessionToken, SyncResult? syncResultForUpdating)
        {
            var sessionInfo = sessionManager.GetOrOpenSession(training.Username, sessionToken);
            if (syncResultForUpdating != null)
            {
                if (sessionInfo.Error != GetOrCreateSessionResult.ErrorTypes.OK)
                    syncResultForUpdating.error = sessionInfo.Error.ToString();
                syncResultForUpdating.sessionToken = sessionInfo.Session.SessionToken;
            }

            if (sessionInfo.Session.UserRepositories == null)
                sessionInfo.Session.UserRepositories = userGeneratedRepositoriesFactory.Create(training.Id);

            return sessionInfo.Session.UserRepositories!;
        }

        private async Task DispatchIncoming(Training training, SyncInput root)
        {
            try
            {
                // E.g. for real-time teacher view
                await eventDispatcher.Dispatch(new TrainingSyncMessage
                {
                    TrainingId = training.Id,
                    Username = training.Username,
                    ClientTimestamp = DateTimeOffset.FromUnixTimeMilliseconds(root.CurrentTime),
                    ReceivedTimestamp = DateTimeOffset.UtcNow,
                    // Goddamn System.Text.Json, serializing to ValueKind bullsh*t...
                    Data = root.Events
                        .Where(o => LogItem.GetEventClassName(o) != "UserStatePushLogItem")
                        .Select(o => o.ToString()).OfType<string>()
                        .Select(o => { try { return JObject.Parse(o); } catch { return null; } }).OfType<JObject>()
                });
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Dispatcher");
            }
        }

        public async Task<SyncResult> Sync(Training training, SyncInput root)
        {
            // Disable this for now (rely on the /ping endpoint being called): await DispatchIncoming(training, root);

            var result = new SyncResult();

            var userRepositories = AssertSession(training, root.SessionToken, result);
            
            var currentStoredState = (await userRepositories.UserStates.GetAll()).SingleOrDefault();

            if (root.Events?.Any() == true)
            {
                var logItems = DeserializeEvents(root.Events, log);
                var userStates = logItems.OfType<UserStatePushLogItem>();

                if (userStates.Any())
                {
                    var pushedStateItem = userStates.Last();
                    log.LogInformation($"Training {training.Id}/'{training.Username}' - day {pushedStateItem.exercise_stats.trainingDay}");
                    if (currentStoredState != null && pushedStateItem.exercise_stats.trainingDay < currentStoredState.exercise_stats.trainingDay)
                        log.LogWarning($"Training id={training.Id} Latest trainingDay in stored data: {currentStoredState.exercise_stats.trainingDay}, incoming: {pushedStateItem.exercise_stats.trainingDay}");
                    else
                    {
                        currentStoredState = new UserGeneratedState { exercise_stats = pushedStateItem.exercise_stats, user_data = pushedStateItem.user_data };
                        await userRepositories.UserStates.Upsert(new[] { currentStoredState });
                    }
                }

                {
                    // Log incoming data - but remove large State items
                    // Warning: modifying incoming data instead of making a copy
                    root.Events = root.Events.Where(o => LogItem.GetEventClassName(o) != "UserStatePushLogItem").ToArray();
                    await dataSink.Log(root.Uuid, root);

                    var errorLogItems = logItems.OfType<ErrorLogItem>().ToList();
                    if (errorLogItems.Any())
                    {
                        var userInfo = $"Training id={training.Id}";
                        log.LogWarning($"{userInfo} errors: {JsonConvert.SerializeObject(errorLogItems)}");
                        log.LogWarning($"{userInfo} info: Version={root.ClientVersion} App={root.ClientApp} Device={JsonConvert.SerializeObject(root.Device)}");
                    }
                }

                try
                {
                    await aggregationService.UpdateAggregates(userRepositories, logItems, training.Id);
                }
                catch (Exception ex)
                {
                    log.LogError(ex, $"UpdateAggregates");
                }

                var modified = await trainingAnalyzers.Execute(training, userRepositories, logItems);
                if (modified)
                {
                    log.LogInformation($"Modified training saved, id = {training.Id}");
                    await trainingRepository.Update(training);
                }
            }

            if (root.RequestState)
            {
                log.LogInformation($"Training id={training.Id} ({training.Username}) RequestState");
                // client wants TrainingPlan, stats for trained exercises, training day number etc
                result.state = await CreateClientState(root, training, currentStoredState);

                try
                {
                    var trainingDays = await userRepositories.TrainingDays.GetAll();
                    if (trainingDays.Any() && currentStoredState != null)
                    {
                        var fromTrainingDays = trainingDays.Max(o => o.TrainingDay);
                        if (currentStoredState.exercise_stats.trainingDay != fromTrainingDays)
                        {
                            log.LogWarning($"Training id={training.Id} day mismatch - state:{currentStoredState.exercise_stats.trainingDay} td table:{fromTrainingDays}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.LogError(ex, $"Compare days error Training id={training.Id}");
                }
            }

            return result;
        }

        private async Task<string> CreateClientState(SyncInput root, Training training, UserGeneratedState? currentStoredState)
        {
            var trainingPlanName = string.IsNullOrEmpty(training.TrainingPlanName) ? "2017 HT template Default" : training.TrainingPlanName; // "testplan";
            var trainingPlan = await trainingPlanRepository.Get(trainingPlanName);
            if (trainingPlan == null)
                throw new Exception($"Training plan '{trainingPlanName}' does not exist");

            var fullState = JObject.FromObject(new UserFullState
            {
                uuid = root.Uuid,
                training_plan = trainingPlan,
                training_settings = training.Settings
            });

            var typedTrainingPlan = JsonConvert.DeserializeObject<TrainingPlan>(JsonConvert.SerializeObject(trainingPlan));
            var clientRequirements = typedTrainingPlan?.clientRequirements;
            if (clientRequirements?.Version != null)
            {
                SemVerHelper.AssertClientVersion(root.ClientVersion?.Split(',')[^1], clientRequirements.Version.Min, clientRequirements.Version.Max);
            }

            if (currentStoredState != null)
            {
                fullState["exercise_stats"] = JObject.FromObject(currentStoredState.exercise_stats);
                fullState["user_data"] = currentStoredState.user_data == null ? null : JObject.FromObject(currentStoredState.user_data);
            }

            return JsonConvert.SerializeObject(fullState);
        }

        private static List<LogItem> DeserializeEvents(object[] events, ILogger? log = null)
        {
            var unhandledItems = new List<object>();
            var result = events.Select(item =>
            {
                var (logItem, error) = LogItem.TryDeserialize(item);
                if (error is not null)
                    unhandledItems.Add(new { Exception = error, Item = item });
                return logItem;
            }).OfType<LogItem>()
            .ToList();

            if (unhandledItems.Any())
            {
                if (log != null)
                    log.LogError($"Deserializing problems:\n{JsonConvert.SerializeObject(unhandledItems)}");
                //throw new Exception($"Deserializing problems:\n{JsonConvert.SerializeObject(unhandledItems)}");
            }

            return result;
        }

        private static List<int> GetUserStatePushLogItemIndices(SyncInput root)
        {
            if (root.Events?.Any() == true)
            {
                return root.Events.Select((o, i) => new { Item = o, Index = i })
                    .Where(o => TryOrDefault(() => LogItem.GetEventClassName(o.Item) == "UserStatePushLogItem", false))
                    .Select(o => o.Index).ToList();
            }
            return new List<int>();
        }

        public static T TryOrDefault<T>(Func<T> func, T defaultValue)
        {
            try
            {
                return func();
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}
