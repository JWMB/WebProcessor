using System;
using static ProblemSource.Models.Aggregates.MLFeaturesJulia;

namespace ProblemSource.Models.Aggregates
{
    public class MLFeaturesJulia
    {
        public Dictionary<string, FeaturesForExercise> ByExercise { get; set; } = new();

        // Mean time increase: The difference in response time between the question following an incorrectly answered question and the incorrectly answered question (response time after incorrect minus response time incorrect)
        public int MeanTimeIncrease { get; set; }

        /// <summary>
        /// 12) Training time 20 min: Dummy coded with a 1 if the training time is 20 min and 0 if the training time is 33 min per day.
        /// </summary>
        public bool TrainingTime20Min { get; set; }

        /// <summary>
        /// 13) Age 6 - 7: Dummy coded with a 1 if the age is 6 - 7 and a 0 if the age is 7 - 8(other age groups have been excluded from the data set)
        /// </summary>
        public bool Age6_7 { get; set; }

        public static MLFeaturesJulia FromPhases(TrainingSettings trainingSettings, IEnumerable<Phase> phases, int dayCutoff = 5)
        {
            return new MLFeaturesJulia
            {
                ByExercise = phases.Where(o => o.training_day <= dayCutoff)
                    .GroupBy(o => Phase.GetExerciseCommonName(o.exercise))
                    .ToDictionary(o => o.Key, FeaturesForExercise.Create),
                MeanTimeIncrease = 0,
                TrainingTime20Min = (trainingSettings.timeLimits?.FirstOrDefault() ?? 33) == 20,
                Age6_7 = false, // TODO:
            };
        }

        public string[] ToArray()
        {
            var npals = GetFeatures("npals");
            var wmGrid = GetFeatures("WM_grid");
            var numberline = GetFeatures("numberline");
            var mathTest01 = GetFeatures("mathTest01");
            var nvr_rp = GetFeatures("nvr_rp");
            var nvr_so = GetFeatures("nvr_so");
            var numberComparison01 = GetFeatures("numberComparison01");
            var tangram = GetFeatures("tangram");
            var rotation = GetFeatures("rotation");

            return new object[][] {
                new[] { npals, wmGrid, numberline, mathTest01, nvr_rp, nvr_so, numberComparison01 }
                    .Select(o => (object)o.PercentCorrect).ToArray(),

                // Note: tangram instead of wmGrid
                new[] { npals, tangram, numberline, mathTest01, nvr_rp, nvr_so, numberComparison01 }
                    .Select(o => (object)o.NumProblemsWithAnswers).ToArray(),

                new[] { wmGrid, npals, numberline, rotation, nvr_rp, mathTest01, numberComparison01 }
                    .Select(o => (object)o.StandardDeviation).ToArray(),

                new[] { npals, numberline, nvr_so, nvr_rp }
                    .Select(o => (object)o.HighestLevel).ToArray(),

                new[] { npals, tangram, numberline, rotation, nvr_rp }
                    .Select(o => (object)o.NumExercises).ToArray(),

                new[]{ (object)MeanTimeIncrease },

                // Note: description for NVR SO slightly different - "time correct" instead of "median time correct"
                new[] { tangram, rotation, nvr_so, mathTest01, numberComparison01 }
                    .Select(o => (object)o.MedianTimeCorrect).ToArray(),

                new[] { wmGrid, npals, rotation, mathTest01 }
                    .Select(o => (object)o.MedianTimeIncorrect).ToArray(),

                new[] { npals, rotation, numberline, nvr_rp, nvr_so, numberComparison01 }
                    .Select(o => (object)o.NumHighResponseTimes).ToArray(),

                new[] { mathTest01, npals, nvr_rp, nvr_so, rotation, tangram }
                    .Select(o => (object)o.Skew).ToArray(),

                new[] { npals, numberline, nvr_rp }
                    .Select(o => (object)o.MedianLevel).ToArray(),

                new[]{ (object)(TrainingTime20Min ? 1 : 0) },

                new[]{ (object)(Age6_7 ? 1 : 0) },

            }.SelectMany(o => o).Select(o => o.ToString() ?? "").ToArray();

            FeaturesForExercise GetFeatures(string exercise) => ByExercise.GetValueOrDefault(exercise, new FeaturesForExercise()); 
        }

        public class FeaturesForExercise
        {
            public int PercentCorrect { get; set; }
            public int NumProblemsWithAnswers { get; set; }
            public decimal StandardDeviation { get; set; }
            public decimal HighestLevel { get; set; }
            public int NumExercises { get; set; }

            public int MedianTimeCorrect { get; set; }
            public int MedianTimeIncorrect { get; set; }
            
            public int NumHighResponseTimes { get; set; }

            public int Skew { get; set; }

            public decimal MedianLevel { get; set; }

            public static FeaturesForExercise Create(IEnumerable<Phase> phases)
            {
                var stats = new FeaturesForExercise();

                // Calc pre-requisites
                var allProblems = phases.SelectMany(phase => phase.problems).ToList();
                if (allProblems.Count == 0)
                    return stats;

                var responseTimes = allProblems.SelectMany(o => o.answers.Select(o => (decimal)o.response_time)) // Note: doesn't care if correct or not
                    .Select(o => Math.Min(o, 60000)) // cap at 60 seconds
                    .Order()
                    .ToList();

                if (responseTimes.Count == 0)
                    return stats;

                var lnResponseTimes = responseTimes.Select(t => (decimal)Math.Log((double)t));
                var cutoff = lnResponseTimes.GetMedian() + 2.5M * lnResponseTimes.GetStandardDeviation();
                // TODO: easier to un-log cutoff

                Func<decimal, bool> isNotOutlier = val => (decimal)Math.Log((double)val) <= cutoff;
                Func<decimal, bool> isOutlier = val => (decimal)Math.Log((double)val) > cutoff;

                stats.NumProblemsWithAnswers = allProblems.Count(problem => problem.answers.Any());
                stats.PercentCorrect = 100 * allProblems.Count(problem => problem.answers.Any(answer => answer.correct)) / allProblems.Count();

                // Standard deviation:
                stats.StandardDeviation = responseTimes.Where(isNotOutlier).GetStandardDeviation();

                // Highest level reached (with at least one correct answered on that level)
                stats.HighestLevel = allProblems
                    .Where(HasCorrectAnswer)
                    .Max(problem => problem.level);

                var orderedPhases = phases.OrderBy(p => $"{p.training_day.ToString().PadLeft(3, '0')}_{p.time}").ToList();
                // Number of exercises: The number of exercises it took to reach the highest level defined above
                // TODO: just reach level, or with correct answer?
                stats.NumExercises = 1 + orderedPhases.FindIndex(phase => phase.problems.Any(p => p.level == stats.HighestLevel));

                // 7) Median time correct: The median response time for correctly answered questions after outliers have been removed
                // for exercise = Tangram, Rotation, NVR SO, Mathtest01 and Numbercomparison01
                stats.MedianTimeCorrect = (int)allProblems
                    .Where(HasCorrectAnswer)
                    .Select(o => (decimal)o.answers.First().response_time)
                    .Where(isNotOutlier)
                    .GetMedian();

                // 8) Median time incorrect: The median response time for correctly answered questions minus the median response time for incorrectly answered questions after outliers have been removed
                // for exercise = WM - Grid, Npals, Rotation and Mathtest01
                stats.MedianTimeIncorrect =
                    stats.MedianTimeCorrect - (int)allProblems
                    .Where(problem => HasCorrectAnswer(problem) == false)
                    .Select(o => (decimal)o.answers.First().response_time)
                    .Where(isNotOutlier)
                    .GetMedian();
                //TODO: not sure this is what is expected

                //9) Number of high response times: The number of questions with a response time above the outlier cutoff
                //for exercise = Npals, Rotation, Numberline, NVR RP, NVR SO and Numbercomparison01
                stats.NumHighResponseTimes = allProblems.Count(problem => problem.answers.Any(answer => isOutlier(answer.response_time)));

                //10) Skew: The skew for response times after outliers have been removed
                //for exercise = Mathtest01, Npals, NVR RP, NVR SO, Rotation and Tangram 

                //11) Median level: The median level of correctly answered questions
                //for exercise = Npals, Numberline and NVR RP
                stats.MedianLevel = allProblems.Where(problem => problem.answers.Any(answer => answer.correct))
                    .Select(problem => problem.level)
                    .Order()
                    .GetMedian();

                return stats;

                bool HasCorrectAnswer(Problem problem) => problem.answers.Any(answer => answer.correct);
            }

        }
    }

    public static class StatisticsExtensions
    {
        public static decimal GetMedian(this IEnumerable<decimal> values)
        {
            if (values is not IOrderedEnumerable<decimal>)
                values = values.Order().ToArray();
            var enumerable = values as decimal[] ?? values.ToArray();
            var count = enumerable.Count();
            if (count == 0)
                return 0;
            return enumerable[count / 2];
        }
        public static decimal GetStandardDeviation(this IEnumerable<decimal> values)
        {
            var enumerable = values as decimal[] ?? values.ToArray();
            var count = enumerable.Count();
            if (count > 1)
            {
                var avg = enumerable.Average();
                var sum = enumerable.Sum(d => (d - avg) * (d - avg));
                return (decimal)Math.Sqrt((double)(sum / count));
            }
            return 0;
        }
    }
}
