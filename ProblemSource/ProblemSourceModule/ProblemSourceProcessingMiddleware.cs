using Common;
using Common.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PluginModuleBase;
using ProblemSource.Models;
using ProblemSource.Models.LogItems;
using ProblemSource.Services;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Models;
using ProblemSourceModule.Services.Storage;

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
        private readonly ILogger<ProblemSourceProcessingMiddleware> log;

        //public bool SupportsMiddlewarePattern => throw new NotImplementedException();

        public ProblemSourceProcessingMiddleware(ITrainingPlanRepository trainingPlanRepository,
            IClientSessionManager sessionManager, IDataSink dataSink, IEventDispatcher eventDispatcher, IAggregationService aggregationService,
            IUserGeneratedDataRepositoryProviderFactory userGeneratedRepositoriesFactory, UsernameHashing usernameHashing, MnemoJapanese mnemoJapanese,
            ITrainingRepository trainingRepository,
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
            this.log = log;
        }

        public async Task Invoke(HttpContext context, RequestDelegate next)
        {
            SyncInput? root;
            if (context.Request.Headers.ContentType.Any(o => o?.ToLower().Contains("application/json") == true))
            {
                root = await context.Request.ReadFromJsonAsync<SyncInput>();
            }
            else
            {
                var body = await context.Request.ReadBodyAsStringAsync();
                if (string.IsNullOrEmpty(body))
                    throw new ArgumentNullException($"Request.Body");
                root = System.Text.Json.JsonSerializer.Deserialize<SyncInput?>(body);
            }
            
            if (root == null)
                throw new ArgumentException("input: incorrect format");

            var result = await Process(root, context.User);

            await next.Invoke(context);

            await context.Response.WriteAsJsonAsync(result);
        }

        private async Task<SyncResult> Process(SyncInput root, System.Security.Claims.ClaimsPrincipal? user)
        {
            // TODO: use regular model validation
            if (string.IsNullOrEmpty(root.Uuid))
            {
                throw new ArgumentNullException($"{nameof(root.Uuid)}");
            }
            if (user?.Claims.Any() == false  // anonymous access for "validate" request
    && root.SessionToken == "validate") // TODO: co-opting SessionToken for now
            {
                var dehashedUuid = usernameHashing.Dehash(root.Uuid);
                if (dehashedUuid == null)
                {
                    if (!root.Uuid.Contains(" ") && root.Uuid.Length > 4) // allow for forgotten space
                        dehashedUuid = usernameHashing.Dehash(root.Uuid.Insert(4, " "));

                    if (dehashedUuid == null)
                        return new SyncResult { error = $"Username not found: '{root.Uuid}'" };
                }
                return new SyncResult { messages = $"redirect:/index2.html?autologin={root.Uuid}" };
            }

            // TODO: client has already dehashed (but should not, let server handle ui)
            var id = mnemoJapanese.ToIntWithRandom(root.Uuid);
            if (id == null)
                return new SyncResult { error = $"Username not found ({root.Uuid})" };

            if (user == null) // For actual sync, we require an authenticated user
                throw new Exception("Unauthenticated"); // TODO: some HttpException with status code

            var training = await trainingRepository.Get(id.Value);
            if (training == null)
                return new SyncResult { error = $"Username not found ({root.Uuid}/{id.Value})" };

            return await Sync(root);
        }

        private async Task<(Training? training, string? error)> GetTrainingFromInput(SyncInput root)
        {
            if (string.IsNullOrEmpty(root.Uuid))
                return (null, $"Username empty");

            var trainingId = mnemoJapanese.ToIntWithRandom(root.Uuid);
            if (trainingId == null)
            {
                var dehashedUuid = usernameHashing.Dehash(root.Uuid);
                if (dehashedUuid != null)
                    trainingId = mnemoJapanese.ToIntWithRandom(dehashedUuid);
                if (trainingId == null)
                    return (null, $"Username not found ({root.Uuid})");
            }

            var training = await trainingRepository.Get(trainingId.Value);
            return (training, training == null ? $"Username not found ({root.Uuid}/{trainingId.Value})" : null);
        }

        public async Task<SyncResult> Sync(SyncInput root)
        {
            var result = new SyncResult();

            var (training, error) = await GetTrainingFromInput(root);
            if (training == null)
            {
                return new SyncResult { error = error };
            }

            var sessionInfo = sessionManager.GetOrOpenSession(root.Uuid, root.SessionToken);
            if (sessionInfo.Error != GetOrCreateSessionResult.ErrorTypes.OK)
                result.error = sessionInfo.Error.ToString();
            result.sessionToken = sessionInfo.Session.SessionToken;

            if (sessionInfo.Session.UserRepositories == null)
            {
                sessionInfo.Session.UserRepositories = userGeneratedRepositoriesFactory.Create(training.Id);
            }

            await eventDispatcher.Dispatch(root.Events); // E.g. for real-time teacher view

            var currentStoredState = (await sessionInfo.Session.UserRepositories.UserStates.GetAll()).SingleOrDefault();

            if (root.Events?.Any() == true)
            {
                var logItems = DeserializeEvents(root.Events, log);
                var userStates = logItems.OfType<UserStatePushLogItem>();

                if (userStates.Any())
                {
                    var pushedStateItem = userStates.Last();
                    if (currentStoredState != null && pushedStateItem.exercise_stats.trainingDay < currentStoredState.exercise_stats.trainingDay)
                        log.LogWarning($"({training.Id}) Latest trainingDay in stored data: {currentStoredState.exercise_stats.trainingDay}, incoming: {pushedStateItem.exercise_stats.trainingDay}");
                    else
                    {
                        currentStoredState = new UserGeneratedState { exercise_stats = pushedStateItem.exercise_stats, user_data = pushedStateItem.user_data };
                        await sessionInfo.Session.UserRepositories.UserStates.Upsert(new[] { currentStoredState });
                    }
                }

                {
                    // Log incoming data - but remove large State items
                    // Warning: modifying incoming data instead of making a copy
                    root.Events = root.Events.Where(o => LogItem.GetEventClassName(o) != "UserStatePushLogItem").ToArray();

                    await dataSink.Log(root.Uuid, root);
                }

                try
                {
                    await aggregationService.UpdateAggregates(sessionInfo.Session.UserRepositories, logItems, training.Id);
                }
                catch (Exception ex)
                {
                    log.LogError(ex, $"UpdateAggregates");
                }

                // TODO: check for EndOfDayLogItem (or if TrainingDay has changed) to trigger 
            }

            if (root.RequestState)
            {
                // client wants TrainingPlan, stats for trained exercises, training day number etc
                result.state = await CreateClientState(root, training, currentStoredState);
            }

            return result;
        }

        private async Task<string> CreateClientState(SyncInput root, Training training, UserGeneratedState? currentStoredState)
        {
            var trainingPlanName = string.IsNullOrEmpty(training.TrainingPlanName) ? "2017 HT template Default" : training.TrainingPlanName; // "testplan";
            var trainingPlan = await trainingPlanRepository.Get(trainingPlanName);
            if (trainingPlan == null)
                throw new Exception($"Training plan '{trainingPlanName}' does not exist");

            var fullState = Newtonsoft.Json.Linq.JObject.FromObject(new UserFullState
            {
                uuid = root.Uuid,
                training_plan = trainingPlan,
                training_settings = training.Settings ?? new TrainingSettings
                {
                    timeLimits = new List<decimal> { 33 },
                    customData = new CustomData { unlockAllPlanets = false }
                }
            });

            var typedTrainingPlan = JsonConvert.DeserializeObject<TrainingPlan>(JsonConvert.SerializeObject(trainingPlan));
            var clientRequirements = typedTrainingPlan?.clientRequirements;
            if (clientRequirements?.Version != null)
            {
                SemVerHelper.AssertClientVersion(root.ClientVersion?.Split(',')[^1], clientRequirements.Version.Min, clientRequirements.Version.Max);
            }

            if (currentStoredState != null)
            {
                fullState["exercise_stats"] = JsonConvert.SerializeObject(currentStoredState.exercise_stats);
                fullState["user_data"] = JsonConvert.SerializeObject(currentStoredState.user_data);
            }

            // TODO: apply rules engine - should e.g. training plan be modified?

            return JsonConvert.SerializeObject(fullState);
        }

        //interface ITrainingModifierEngine
        //{
        //    void Run(Training training);
        //}
        //class TrainingModifierEngine : ITrainingModifierEngine
        //{
        //    public void Run(Training training)
        //    {
        //    }
        //    class Day5Switcher
        //    {
        //        public void Run(Training training)
        //        {
        //            var trainingDay = 5;
        //            if (trainingDay == 5)
        //            {
        //                //training.Settings.trainingPlanOverrides
        //            }
        //        }
        //    }
        //}

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
