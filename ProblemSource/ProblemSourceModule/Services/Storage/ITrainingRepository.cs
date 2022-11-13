namespace ProblemSourceModule.Services.Storage
{

    public interface ITrainingRepository : IRepository<Training, int>
    {
    }

    public class Training
    {
        public int Id { get; set; }
        public string TrainingPlanName { get; set; } = string.Empty;
    }
}
