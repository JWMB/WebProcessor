using Common;
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
        public DateTime end_timestamp { get; set; }

        public int sequence { get; set; }

        public int num_questions { get; set; }
        public int num_correct_first_try { get; set; }
        public int num_correct_answers { get; set; }
        public int num_incorrect_answers { get; set; }

        public decimal level_min { get; set; }
        public decimal level_max { get; set; }

        public int response_time_avg { get; set; }
        public int response_time_total { get; set; }

        public bool? won_race { get; set; }
        public bool? completed_planet { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj == null || obj is PhaseStatistics typed == false)
                return false;
            return training_day == typed.training_day && exercise == typed.exercise && phase_type == typed.phase_type && timestamp == typed.timestamp;
        }

        public override int GetHashCode() => $"{id} {phase_id} {training_day}".GetHashCode();

        //Score,Target score,Planet target score
        public static string UniqueIdWithinUser(PhaseStatistics p) => $"{p.training_day}_{p.exercise.Replace("#", "")}_{Math.Abs(p.timestamp.ToUnixTimestamp())}";

        public static List<PhaseStatistics> Create(int accountId, IEnumerable<Phase> phases)
        {
            return phases.Select(phase =>
            {
                var lastAnswers = phase.problems.OrderBy(o => o.time).Select(o => o.answers?.LastOrDefault()).OfType<Answer>();
                var lastTimestamp = new[] { phase.time, phase.problems.LastOrDefault()?.time ?? 0, lastAnswers.LastOrDefault()?.time ?? 0 }.Max();
                return new PhaseStatistics
                {
                    account_id = accountId,
                    training_day = phase.training_day,
                    exercise = phase.exercise,
                    phase_type = phase.phase_type,
                    timestamp = new DateTime(1970, 1, 1).AddMilliseconds(Math.Max(0, phase.time)),
                    end_timestamp = new DateTime(1970, 1, 1).AddMilliseconds(Math.Max(0, lastTimestamp)),
                    sequence = phase.sequence,

                    num_questions = phase.problems.Count,
                    num_correct_first_try = phase.problems.Select(o => o.answers.FirstOrDefault()).Count(o => o?.correct == true),
                    num_correct_answers = phase.problems.Count(o => o.answers.Exists(p => p.correct)),
                    num_incorrect_answers = phase.problems.SelectMany(o => o.answers).Count(o => o.correct == false),

                    level_min = phase.problems.Any() ? phase.problems.Min(o => o.level) : 0,
                    level_max = phase.problems.Any() ? phase.problems.Max(o => o.level) : 0,

                    response_time_avg = lastAnswers.Any() ? (int)lastAnswers.Average(o => o.response_time) : 0,
                    response_time_total = lastAnswers.Any() ? lastAnswers.Sum(o => o.response_time) : 0,

                    won_race = phase.user_test?.won_race,
                    completed_planet = phase.user_test?.completed_planet,
                };
            }).ToList();
        }
    }
}
