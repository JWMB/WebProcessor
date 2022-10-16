using System.ComponentModel.DataAnnotations;

namespace ProblemSource.Models.Aggregates
{
    public class PhaseStatistics
    {
        [Key]
        public int id { get; set; }
        public int phase_id { get; set; }
        public int account_id { get; set; }

        public int training_day { get; set; }
        public string exercise { get; set; } = string.Empty;
        public string phase_type { get; set; } = string.Empty;
        public DateTime timestamp { get; set; }
        public int sequence { get; set; }

        public int num_questions { get; set; }
        public int num_correct_first_try { get; set; }
        public int num_correct_answers { get; set; }
        public int num_incorrect_answers { get; set; }

        public decimal level_min { get; set; }
        public decimal level_max { get; set; }

        public int response_time_avg { get; set; }
        public int response_time_total { get; set; }

        //Score,Target score,Planet target score,Won race,Completed planet

        public static List<PhaseStatistics> Create(int accountId, IEnumerable<Phase> phases)
        {
            return phases.Select(phase =>
            {
                var lastAnswers = phase.problems.Select(o => o.answers?.LastOrDefault()).Where(o => o != null);
                return new PhaseStatistics
                {
                    account_id = accountId,
                    training_day = phase.training_day,
                    exercise = phase.exercise,
                    phase_type = phase.phase_type,
                    timestamp = new DateTime(1970, 1, 1).AddMilliseconds(phase.time),
                    sequence = phase.sequence,

                    num_questions = phase.problems.Count,
                    num_correct_first_try = phase.problems.Select(o => o.answers.FirstOrDefault()).Count(o => o?.correct == true),
                    num_correct_answers = phase.problems.Count(o => o.answers.Exists(p => p.correct)),
                    num_incorrect_answers = phase.problems.SelectMany(o => o.answers).Count(o => o.correct == false),

                    level_min = phase.problems.Any() ? phase.problems.Min(o => o.level) : 0,
                    level_max = phase.problems.Any() ? phase.problems.Max(o => o.level) : 0,

                    response_time_avg = lastAnswers.Any() ? (int)lastAnswers.Average(o => o.response_time) : 0,
                    response_time_total = lastAnswers.Any() ? lastAnswers.Sum(o => o.response_time) : 0,
                };
            }).ToList();
        }
    }
}
