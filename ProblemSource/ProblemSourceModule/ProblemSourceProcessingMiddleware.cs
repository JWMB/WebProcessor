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
using ProblemSourceModule.Services.Storage;

namespace ProblemSource
{
    public class ProblemSourceProcessingMiddleware : IProcessingMiddleware
    {
        private readonly IUserStateRepository userStateRepository;
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

        public ProblemSourceProcessingMiddleware(IUserStateRepository userStateRepository, ITrainingPlanRepository trainingPlanRepository,
            IClientSessionManager sessionManager, IDataSink dataSink, IEventDispatcher eventDispatcher, IAggregationService aggregationService,
            IUserGeneratedDataRepositoryProviderFactory userGeneratedRepositoriesFactory, UsernameHashing usernameHashing, MnemoJapanese mnemoJapanese,
            ITrainingRepository trainingRepository,
            ILogger<ProblemSourceProcessingMiddleware> log)
        {
            this.userStateRepository = userStateRepository;
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

        public async Task<SyncResult> Sync(SyncInput root)
        {
            var result = new SyncResult();

            if (string.IsNullOrEmpty(root.Uuid))
                return new SyncResult { error = $"Username empty" };

            var trainingId = mnemoJapanese.ToIntWithRandom(root.Uuid);
            if (trainingId == null)
            {
                var dehashedUuid = usernameHashing.Dehash(root.Uuid);
                if (dehashedUuid != null)
                    trainingId = mnemoJapanese.ToIntWithRandom(dehashedUuid);
                if (trainingId == null)
                    return new SyncResult { error = $"Username not found ({root.Uuid})" };
            }

            var training = await trainingRepository.Get(trainingId.Value);
            if (training == null)
                return new SyncResult { error = $"Username not found ({root.Uuid}/{trainingId.Value})" };

            var sessionInfo = sessionManager.GetOrOpenSession(root.Uuid, root.SessionToken);
            if (sessionInfo.Error != GetOrCreateSessionResult.ErrorTypes.OK)
                result.error = sessionInfo.Error.ToString();
            result.sessionToken = sessionInfo.Session.SessionToken;

            await eventDispatcher.Dispatch(root.Events); // E.g. for real-time teacher view

            if (root.Events?.Any() == true)
            {
                var logItems = DeserializeEvents(root.Events, log);
                var userStates = logItems.OfType<UserStatePushLogItem>();
                if (userStates.Any())
                {
                    await HandleUserState(root.Uuid, userStates.Last());
                }

                {
                    // Log incoming data - but remove large State items
                    // Warning: modifying incoming data instead of making a copy
                    root.Events = root.Events.Where(o => GetEventClassName(o) != "UserStatePushLogItem").ToArray();

                    await dataSink.Log(root.Uuid, root);
                }

                if (sessionInfo.Session.UserRepositories == null)
                {
                    sessionInfo.Session.UserRepositories = userGeneratedRepositoriesFactory.Create(trainingId.Value);
                }
                try
                {
                    await aggregationService.UpdateAggregates(sessionInfo.Session.UserRepositories, logItems, trainingId.Value);
                }
                catch (Exception ex)
                {
                    log.LogError(ex, $"UpdateAggregates");
                }
            }

            if (root.RequestState)
            {
                // client wants TrainingPlan, stats for trained exercises, training day number etc
                result.state = await CreateClientState(root, training);
            }

            return result;
        }

        private async Task<string> CreateClientState(SyncInput root, Training training)
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

            var storedUserState = await userStateRepository.Get(root.Uuid);
            if (storedUserState != null)
            {
                var d = storedUserState as dynamic;
                if (d != null)
                {
                    try
                    {
                        fullState["exercise_stats"] = d.exercise_stats;
                        fullState["user_data"] = d.user_data;
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex, "Why do we think there'll be an exception here?");
                    }
                }
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

        private async Task HandleUserState(string uuid, UserStatePushLogItem lastItem)
        {
            // TODO: What? Why serialize/deserialize?
            //var asJson = JsonConvert.SerializeObject(lastItem);
            //var pushedStateItem = JsonConvert.DeserializeObject<UserStatePushLogItem>(asJson);
            var pushedStateItem = lastItem;

            if (pushedStateItem == null)
            {
                log.LogWarning($"({uuid}) Couldn't deserialize pushedStateItem");
            }
            else
            {
                var state = await userStateRepository.Get<UserGeneratedState>(uuid);
                if (state != null && pushedStateItem.exercise_stats.trainingDay < state.exercise_stats.trainingDay)
                {
                    log.LogWarning($"({uuid}) Latest trainingDay in stored data: {state.exercise_stats.trainingDay}, incoming: {pushedStateItem.exercise_stats.trainingDay}");
                }
                else
                {
                    //var asDynamic = JsonConvert.DeserializeObject<dynamic>(asJson); // so as to keep any non-typed properties/data
                    //await userStateRepository.Set(uuid, new { exercise_stats = asDynamic!.exercise_stats, user_data = asDynamic.user_data });
                    await userStateRepository.Set(uuid, new { exercise_stats = lastItem.exercise_stats, user_data = lastItem.user_data });
                }
            }
        }

        private static string? GetEventClassName(object item)
        {
            // what the hell? Suddenly the deserialized items are JsonElement instead of dynamic object
            if (item is System.Text.Json.JsonElement jEl)
                return jEl.GetProperty("className").GetString();
            else
                return ExceptionTools.TryOrDefault(() => (string)((dynamic)item).className, null);
        }

        private static List<LogItem> DeserializeEvents(object[] events, ILogger? log = null)
        {
            var nameToType = typeof(LogItem).Assembly.GetTypes().Where(o => typeof(LogItem).IsAssignableFrom(o)).ToDictionary(o => o.Name, o => o);
            var unhandledItems = new List<object>();
            var result = events.Select(item =>
            {
                var className = GetEventClassName(item);
                if (className != null && nameToType.TryGetValue(className, out var type))
                {
                    try
                    {
                        var asJson = item is System.Text.Json.JsonElement jEl ? jEl.ToString() : JsonConvert.SerializeObject(item);
                        var typed = JsonConvert.DeserializeObject(asJson!, type);
                        if (typed is LogItem li)
                            return typed as LogItem;
                    }
                    catch (Exception ex)
                    {
                        unhandledItems.Add(new { Exception = ex, Item = item });
                        return null;
                    }
                }
                unhandledItems.Add(item);
                return null;

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
                    .Where(o => TryOrDefault(() => GetEventClassName(o.Item) == "UserStatePushLogItem", false))
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
