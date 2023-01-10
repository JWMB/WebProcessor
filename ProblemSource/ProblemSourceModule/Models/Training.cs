using ProblemSource.Models;

namespace ProblemSourceModule.Models
{
    public class Training
    {
        public int Id { get; set; }
        public string TrainingPlanName { get; set; } = string.Empty;
        public TrainingSettings? Settings { get; set; }
    }
}
