using Azure.Data.Tables;
using ProblemSource.Models;

namespace ProblemSource.Services
{
    public interface ITrainingPlanRepository
    {
        Task<TrainingPlan?> Get(string name);
    }

    public class TrainingPlanRepository : ITrainingPlanRepository
    {
        //private readonly DirectoryInfo contentRoot;
        //public TrainingPlanRepository(DirectoryInfo contentRoot)
        //{
        //    this.contentRoot = contentRoot;
        //}
        public async Task<TrainingPlan?> Get(string name)
        {
            var resourceName = $"{GetType().Assembly.GetName().Name}.Resources.{name}.json";
            var resource = GetType().Assembly.GetManifestResourceInfo(resourceName);
            if (resource == null)
                return null;

            using (var stream = GetType().Assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream!))
            {
                var tpDef = await reader.ReadToEndAsync();

                var tp = Newtonsoft.Json.JsonConvert.DeserializeObject<LinearTrainingPlan>(tpDef) as TrainingPlan;
                if (tp == null)
                    throw new Exception("TP invalid");
                return tp;
            }

            //var fi = contentRoot.GetFiles($"data/{name}.json").SingleOrDefault();
            //if (fi?.Exists != true)
            //    return null;
            //var tpDef = await File.ReadAllTextAsync(fi.FullName);
            //var tp = Newtonsoft.Json.JsonConvert.DeserializeObject<LinearTrainingPlan>(tpDef) as TrainingPlan;
            //if (tp == null)
            //    throw new Exception("TP invalid");
            //return tp;
        }
    }

    public class AzureTableTrainingPlanRepository : ITrainingPlanRepository
    {
        private readonly TableClient client;

        public AzureTableTrainingPlanRepository(AzureTableConfig config)
        {
            client = config.CreateTableClient(config.TableTrainingPlans);
        }

        public async Task<TrainingPlan?> Get(string name)
        {
            await client.CreateIfNotExistsAsync();
            var partitionKey = name;
            var queryResultsFilter = client.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{partitionKey}'"); //and RowKey eq '{uuid}'

            await foreach (var entity in queryResultsFilter)
            {
                var str = AzureTableConfig.GetLongString(entity);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<LinearTrainingPlan>(str);
            }

            return null;
        }
    }
}
