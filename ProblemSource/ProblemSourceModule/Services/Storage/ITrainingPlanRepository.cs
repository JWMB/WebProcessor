using ProblemSource.Models;

namespace ProblemSource.Services.Storage
{
    public interface ITrainingPlanRepository
    {
        // TODO: need to look into the types here - not sure what the client actually uses (and it seems pretty dynamic)
        Task<object?> Get(string name);
    }

    public class TrainingPlanRepository : ITrainingPlanRepository
    {
        public async Task<object?> Get(string name)
        {
            var resourceName = $"{GetType().Assembly.GetName().Name}.Resources.{name}.json";
            var resource = GetType().Assembly.GetManifestResourceInfo(resourceName);
            if (resource == null)
                return null;

            using (var stream = GetType().Assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream!))
            {
                var tpDef = await reader.ReadToEndAsync();

                var tp = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(tpDef); //LinearTrainingPlan as TrainingPlan;
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
}
