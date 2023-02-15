using ProblemSource.Models;

namespace ProblemSourceModule.Models
{
    public class Training
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string TrainingPlanName { get; set; } = string.Empty;
        public TrainingSettings Settings { get; set; } = TrainingSettings.Default;
        public string AgeBracket { get; set; } = string.Empty;
    }
}
