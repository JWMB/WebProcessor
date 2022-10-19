using Newtonsoft.Json;
using PluginModuleBase;
using ProblemSource.Models;
using ProblemSource.Models.LogItems;
using ProblemSource.Services;
using ProblemSource.Services.Storage;

namespace ProblemSource
{
    public class ProblemSourceProcessingPipeline : IProcessingPipeline
    {
        private readonly IUserStateRepository userStateRepository;
        private readonly ITrainingPlanRepository trainingPlanRepository;
        private readonly IClientSessionManager sessionManager;
        private readonly IDataSink dataSink;
        private readonly IEventDispatcher eventDispatcher;
        private readonly IAggregationService aggregationService;
        private readonly UserGeneratedDataRepositoriesProviderFactory userGeneratedRepositoriesFactory;

        public ProblemSourceProcessingPipeline(IUserStateRepository userStateRepository, ITrainingPlanRepository trainingPlanRepository,
            IClientSessionManager sessionManager, IDataSink dataSink, IEventDispatcher eventDispatcher, IAggregationService aggregationService,
            UserGeneratedDataRepositoriesProviderFactory userGeneratedRepositoriesFactory)
        {
            this.userStateRepository = userStateRepository;
            this.trainingPlanRepository = trainingPlanRepository;
            this.sessionManager = sessionManager;
            this.dataSink = dataSink;
            this.eventDispatcher = eventDispatcher;
            this.aggregationService = aggregationService;
            this.userGeneratedRepositoriesFactory = userGeneratedRepositoriesFactory;
        }

        public async Task<object?> Process(object input)
        {
            if (input is string jsonString)
            {
                return await Sync(jsonString);
            }
            return null;
        }

        public async Task<SyncResult> Sync(string jsonString)
        {
            var result = new SyncResult();

            var root = ParseJson(jsonString);

            var sessionInfo = sessionManager.GetOrOpenSession(root.Uuid, root.SessionToken);
            if (sessionInfo.Error != GetOrCreateSessionResult.ErrorTypes.OK)
                result.error = sessionInfo.Error.ToString();
            result.sessionToken = sessionInfo.Session.SessionToken;

            await eventDispatcher.Dispatch(root.Events); // E.g. for real-time teacher view

            if (root.RequestState)
            {
                // client wants TrainingPlan, stats for trained exercises, training day number etc
                result.state = await CreateClientState(root);
            }

            if (root.Events?.Any() == true)
            {
                var logItems = DeserializeEvents(root.Events);
                var userStates = logItems.OfType<UserStatePushLogItem>();
                if (userStates.Any())
                {
                    await HandleUserState(root.Uuid, userStates.Last());
                }

                {
                    // Log incoming data - but remove large State items
                    // Warning: modifying incoming data instead of making a copy
                    root.Events = root.Events.Where(o => ((dynamic)o).className != "UserStatePushLogItem").ToArray();

                    await dataSink.Log(root.Uuid, root);
                }

                if (sessionInfo.Session.UserRepositories == null)
                {
                    sessionInfo.Session.UserRepositories = userGeneratedRepositoriesFactory.Create(root.Uuid);
                }
                try
                {
                    await aggregationService.UpdateAggregates(sessionInfo.Session.UserRepositories, logItems, root.Uuid);
                }
                catch (Exception ex)
                {
                    // TODO: log
                }
            }

            return result;
        }

        private static SyncInput ParseJson(string jsonString)
        {
            var root = JsonConvert.DeserializeObject<SyncInput>(jsonString);

            if (root == null)
                throw new ArgumentException("Cannot deserialize", nameof(jsonString));
            if (string.IsNullOrEmpty(root.Uuid))
                throw new ArgumentNullException(nameof(root.Uuid));
            return root;
        }

        private async Task<string> CreateClientState(SyncInput root)
        {
            var trainingPlanName = "testplan";
            var trainingPlan = await trainingPlanRepository.Get(trainingPlanName);
            if (trainingPlan == null)
                throw new Exception($"Training plan '{trainingPlanName}' does not exist");

            var fullState = Newtonsoft.Json.Linq.JObject.FromObject(new UserFullState
            {
                uuid = root.Uuid,
                training_plan = trainingPlan,
                training_settings = new TrainingSettings { customData = new CustomData { unlockAllPlanets = true } }
            });

            if (trainingPlan.clientRequirements != null && trainingPlan.clientRequirements.Version != null)
            {
                SemVerHelper.AssertClientVersion(root.ClientVersion?.Split(',')[^1], trainingPlan.clientRequirements.Version.Min, trainingPlan.clientRequirements.Version.Max);
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
                    { }
                }
            }
            return JsonConvert.SerializeObject(fullState);
        }

        private async Task HandleUserState(string uuid, UserStatePushLogItem lastItem)
        {
            var asJson = JsonConvert.SerializeObject(lastItem);
            var pushedStateItem = JsonConvert.DeserializeObject<UserStatePushLogItem>(asJson);

            if (pushedStateItem == null)
            {
                // TODO: log warning
            }
            else
            {
                var state = await userStateRepository.Get<UserGeneratedState>(uuid);
                if (state != null && pushedStateItem.exercise_stats.trainingDay < state.exercise_stats.trainingDay)
                {
                    // TODO: warning - newer data on server
                }
                else
                {
                    var asDynamic = JsonConvert.DeserializeObject<dynamic>(asJson); // so as to keep any non-typed properties/data
                    await userStateRepository.Set(uuid, new { exercise_stats = asDynamic.exercise_stats, user_data = asDynamic.user_data });
                }
            }
        }

        private static List<LogItem> DeserializeEvents(object[] events)
        {
            var nameToType = typeof(LogItem).Assembly.GetTypes().Where(o => typeof(LogItem).IsAssignableFrom(o)).ToDictionary(o => o.Name, o => o);
            return events.Select(item =>
            {
                var className = (string)((dynamic)item).className;
                if (nameToType.TryGetValue(className, out var type))
                {
                    var asJson = JsonConvert.SerializeObject(item);
                    var typed = JsonConvert.DeserializeObject(asJson, type);
                    return typed as LogItem;
                }
                return null;
            }).OfType<LogItem>()
            .ToList();
        }

        private static List<int> GetUserStatePushLogItemIndices(SyncInput root)
        {
            if (root.Events?.Any() == true)
            {
                return root.Events.Select((o, i) => new { Item = o, Index = i })
                    .Where(o => TryOrDefault(() => ((dynamic)o.Item).className == "UserStatePushLogItem", false))
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
            catch (Exception ex)
            {
                return defaultValue;
            }
        }
    }
}
