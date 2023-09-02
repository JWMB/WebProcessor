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

        public DateTimeOffset Created { get; set; }

        public int GetAgeBracketLower() => int.TryParse(AgeBracket.Split('-').Where(o => o.Any()).FirstOrDefault() ?? "6", out var age) ? age : 6;
    }
}
