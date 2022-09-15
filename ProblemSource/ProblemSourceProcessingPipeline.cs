using Newtonsoft.Json;
using PluginModuleBase;
using ProblemSource.Models;
using ProblemSource.Services;

namespace ProblemSource
{
    public class ProblemSourceProcessingPipeline : IProcessingPipeline
    {
        private readonly IUserStateRepository userStateRepository;
        private readonly ITrainingPlanRepository trainingPlanRepository;
        private readonly IClientSessionManager sessionManager;
        private readonly IDataSink dataSink;
        private readonly IEventDispatcher eventDispatcher;

        public ProblemSourceProcessingPipeline(IUserStateRepository userStateRepository, ITrainingPlanRepository trainingPlanRepository,
            IClientSessionManager sessionManager, IDataSink dataSink, IEventDispatcher eventDispatcher)
        {
            this.userStateRepository = userStateRepository;
            this.trainingPlanRepository = trainingPlanRepository;
            this.sessionManager = sessionManager;
            this.dataSink = dataSink;
            this.eventDispatcher = eventDispatcher;
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
            var root = JsonConvert.DeserializeObject<SyncInput>(jsonString);

            if (root == null)
                throw new ArgumentException("Cannot deserialize", nameof(jsonString));
            if (root.ApiKey != "abc")
                throw new ArgumentException("Incorrect value", nameof(root.ApiKey));
            if (string.IsNullOrEmpty(root.Uuid))
                throw new ArgumentNullException(nameof(root.Uuid));

            var result = new SyncResult();

            var sessionInfo = sessionManager.GetOrOpenSession(root.SessionToken);
            if (sessionInfo.Error != GetOrCreateSessionResult.ErrorTypes.OK)
                result.error = sessionInfo.Error.ToString();
            result.sessionToken = sessionInfo.Session.SessionToken;

            await eventDispatcher.Dispatch(root.Events);

            if (root.RequestState)
            {
                var trainingPlanName = "testplan";
                var trainingPlan = await trainingPlanRepository.Get(trainingPlanName);
                if (trainingPlan == null)
                    throw new Exception($"Training plan '{trainingPlanName}' does not exist");

                var fullState = Newtonsoft.Json.Linq.JObject.FromObject(new UserFullState
                {
                    uuid = root.Uuid,
                    training_plan = trainingPlan,
                    training_settings = new TrainingSettings { customData = new CustomData { unlockAllPlanets = true }  }
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
                result.state = JsonConvert.SerializeObject(fullState);
            }

            var stateIndices = GetUserStatePushLogItemIndices(root);
            if (stateIndices.Any())
            {
                var lastItem = root.Events[stateIndices.Last()];
                var asJson = JsonConvert.SerializeObject(lastItem);
                var pushedStateItem = JsonConvert.DeserializeObject<UserStatePushLogItem>(asJson);

                {
                    // Log incoming data - but remove large State items
                    var clearedEvents = new List<object>(root.Events);
                    for (int i = stateIndices.Count - 1; i >= 0; i--)
                        clearedEvents.RemoveAt(stateIndices[i]);
                    root.Events = clearedEvents.ToArray(); // Warning: modifying incoming data instead of making a copy

                    await dataSink.Log(root.Uuid, root);
                }

                if (pushedStateItem == null)
                {
                    // TODO: log warning
                }
                else
                {
                    var state = await userStateRepository.Get<UserGeneratedState>(root.Uuid);
                    if (state != null && pushedStateItem.exercise_stats.trainingDay < state.exercise_stats.trainingDay)
                    {
                        // TODO: warning - newer data on server
                    }
                    else
                    {
                        var asDynamic = JsonConvert.DeserializeObject<dynamic>(asJson); // so as to keep any non-typed properties/data
                        await userStateRepository.Set(root.Uuid, new { exercise_stats = asDynamic.exercise_stats, user_data = asDynamic.user_data });
                    }
                }
            }

            //if (root.Events?.Any() == true)
            //{
            //    for (int i = root.Events.Count() - 1; i >= 0; i--)
            //    {
            //        var s = JsonConvert.SerializeObject(root.Events[i]);
            //        var item = JsonConvert.DeserializeObject<LogItem>(s);
            //        if (item?.className == "UserStatePushLogItem")
            //        {
            //            var pushedStateItem = JsonConvert.DeserializeObject<UserStatePushLogItem>(s);
            //            if (pushedStateItem != null)
            //            {
            //                var state = await userStateRepository.Get<UserGeneratedState>(root.Uuid);
            //                if (state != null && pushedStateItem.exercise_stats.trainingDay < state.exercise_stats.trainingDay)
            //                {
            //                    // TODO: warning - newer data on server
            //                }
            //                else
            //                {
            //                    var asDynamic = JsonConvert.DeserializeObject<dynamic>(s); // so as to keep any non-typed properties/data
            //                    await userStateRepository.Set(root.Uuid, new { exercise_stats = asDynamic.exercise_stats, user_data = asDynamic.user_data });
            //                }
            //            }
            //            break;
            //        }
            //    }
            //}
            return result;
        }

        private List<int> GetUserStatePushLogItemIndices(SyncInput root)
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
        //public static string ToJsonString(JsonDocument jdoc)
        //{
        //    using (var stream = new MemoryStream())
        //    {
        //        var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
        //        jdoc.WriteTo(writer);
        //        writer.Flush();
        //        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        //    }
        //}
    }
}
