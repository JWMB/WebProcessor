using ProblemSource.Models.LogItems;

namespace ProblemSource.Models.Aggregates
{
    public class Problem
    {
        public int id { get; set; }
        public int phase_id { get; set; }
        public decimal level { get; set; }

        public long time { get; set; }
        public string problem_type { get; set; } = string.Empty;
        public string problem_string { get; set; } = string.Empty;

        public List<Answer> answers { get; set; } = new();

        public static Problem Create(NewProblemLogItem newProblem)
        {
            return new Problem
            {
                level = newProblem.level,
                problem_string = newProblem.problem_string,
                problem_type = newProblem.problem_type,
                time = newProblem.time,
            };
        }

        public static Problem CreateUnknown(long time)
        {
            return new Problem
            {
                level = 0,
                problem_string = "N/A",
                problem_type = "N/A",
                time = time,
            };
        }
    }
}
