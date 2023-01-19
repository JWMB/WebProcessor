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

            return new object?[][] {
                new[] { npals, wmGrid, numberline, mathTest01, nvr_rp, nvr_so, numberComparison01 }
                    .ToObjectArray(o => o.PercentCorrect),

                // Note: tangram instead of wmGrid
                new[] { npals, tangram, numberline, mathTest01, nvr_rp, nvr_so, numberComparison01 }
                    .ToObjectArray(o => o.NumProblemsWithAnswers),

                new[] { wmGrid, npals, numberline, rotation, nvr_rp, mathTest01, numberComparison01 }
                    .ToObjectArray(o => o.StandardDeviation),

                new[] { npals, numberline, nvr_so, nvr_rp }
                    .ToObjectArray(o => o.HighestLevel),

                new[] { npals, tangram, numberline, rotation, nvr_rp }
                    .ToObjectArray(o => o.NumExercisesToHighestLevel),

                new[]{ (object)MeanTimeIncrease },

                // Note: description for NVR SO slightly different - "time correct" instead of "median time correct"
                new[] { tangram, rotation, nvr_so, mathTest01, numberComparison01 }
                    .ToObjectArray(o => o.MedianTimeCorrect),

                new[] { wmGrid, npals, rotation, mathTest01 }
                    .ToObjectArray(o => o.MedianTimeIncorrect),

                new[] { npals, rotation, numberline, nvr_rp, nvr_so, numberComparison01 }
                    .ToObjectArray(o => o.NumHighResponseTimes),

                new[] { mathTest01, npals, nvr_rp, nvr_so, rotation, tangram }
                    .ToObjectArray(o => o.Skew),

                new[] { npals, numberline, nvr_rp }
                    .ToObjectArray(o => o.MedianLevel),

                new[]{ (object)(TrainingTime20Min ? 1 : 0) },

                new[]{ (object)(Age6_7 ? 1 : 0) },

            }.SelectMany(o => o).Select(o => o?.ToString() ?? "").ToArray();

            FeaturesForExercise GetFeatures(string exercise) => ByExercise.GetValueOrDefault(exercise, new FeaturesForExercise());
        }

        public class FeaturesForExercise
        {
            public int PercentCorrect { get; set; }
            public int NumProblemsWithAnswers { get; set; }
            public decimal StandardDeviation { get; set; }
            public decimal HighestLevel { get; set; }
            public int NumExercisesToHighestLevel { get; set; }

            public int MedianTimeCorrect { get; set; }
            public int MedianTimeIncorrect { get; set; }
            
            public int NumHighResponseTimes { get; set; }

            public int Skew { get; set; }

            public decimal MedianLevel { get; set; }

            private class ResponseTimesStats
            {
                public decimal MinLevel { get; set; }
                public decimal MaxLevel { get; set; }

                public double OutlierCutOff { get; set; }
                public List<double> ResponseTimes { get; set; } = new();

                public double Mean => ResponseTimes.Average();
                public double MeanNoOutliers => ResponseTimesNoOutliers.Average();

                public IEnumerable<double> ResponseTimesNoOutliers => ResponseTimes.Where(o => o < OutlierCutOff);

                public double StandardDeviationNoOutliers => ResponseTimesNoOutliers.GetStandardDeviation();

                public static ResponseTimesStats Calc(IEnumerable<Problem> problems)
                {
                    var result = new ResponseTimesStats();

                    result.MaxLevel = problems.Max(o => o.level);
                    result.MinLevel = problems.Min(o => o.level);

                    var responseTimes = problems.SelectMany(p => p.answers.Select(a => Math.Min(a.response_time, 60000)));
                    var lnResponseTimes = responseTimes.Select(t => Math.Log(t));
                    result.OutlierCutOff = Math.Exp(lnResponseTimes.GetMedian() + 2.5 * lnResponseTimes.GetStandardDeviation());

                    return result;
                }
            }

            public static FeaturesForExercise Create(IEnumerable<Phase> phases)
            {
                var stats = new FeaturesForExercise();

                // Calc pre-requisites:
                var allProblems = phases.SelectMany(phase => phase.problems).ToList();
                if (allProblems.Count == 0)
                    return stats;

                var responseTimes = allProblems.SelectMany(o => o.answers.Select(o => (double)o.response_time)) // Note: doesn't care if correct or not
                    .Select(o => Math.Min(o, 60000)) // cap at 60 seconds
                    .Order()
                    .ToList();

                if (responseTimes.Count == 0)
                    return stats;

                var responseTimesPerLevel = allProblems.GroupBy(problem => (int)problem.level).ToDictionary(o => o.Key, ResponseTimesStats.Calc);
                var responseTimesTotal = ResponseTimesStats.Calc(allProblems);

                // Calc outputs:

                stats.NumProblemsWithAnswers = allProblems.Count(problem => problem.answers.Any());
                stats.PercentCorrect = 100 * allProblems.Count(problem => problem.answers.Any(answer => answer.correct)) / allProblems.Count();

                stats.StandardDeviation = (decimal)responseTimesPerLevel.Values
                    .Select(o => o.StandardDeviationNoOutliers / o.MeanNoOutliers)
                    .Sum();
                //stats.StandardDeviation = (decimal)responseTimes.Where(isNotOutlier).GetStandardDeviation();

                // Highest level reached (with at least one correct answered on that level)
                stats.HighestLevel = allProblems
                    .Where(HasCorrectAnswer)
                    .Max(problem => problem.level);

                var orderedPhases = phases.OrderBy(p => $"{p.training_day.ToString().PadLeft(3, '0')}_{p.time}").ToList();
                // Number of exercises: The number of exercises it took to reach the highest level defined above
                // TODO: just reach level, or with correct answer?
                stats.NumExercisesToHighestLevel = 1 + orderedPhases.FindIndex(phase => phase.problems.Any(p => p.level == stats.HighestLevel));

                // 7) Median time correct: The median response time for correctly answered questions after outliers have been removed
                stats.MedianTimeCorrect = (int)allProblems
                    .Where(HasCorrectAnswer)
                    .Select(o => (double)o.answers.First().response_time)
                    .Where(isNotOutlier)
                    .GetMedian();

                // 8) Median time incorrect: The median response time for correctly answered questions minus the median response time for incorrectly answered questions after outliers have been removed
                stats.MedianTimeIncorrect =
                    stats.MedianTimeCorrect - (int)allProblems
                    .Where(problem => HasCorrectAnswer(problem) == false)
                    .Select(o => (double)o.answers.First().response_time)
                    .Where(isNotOutlier)
                    .GetMedian();
                //TODO: not sure this is what is expected

                //9) Number of high response times: The number of questions with a response time above the outlier cutoff
                stats.NumHighResponseTimes = allProblems.Count(problem => problem.answers.Any(answer => isOutlier(answer.response_time)));

                //10) Skew: The skew for response times after outliers have been removed
                stats.Skew = 0; // TODO: ?

                //11) Median level: The median level of correctly answered questions
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
        public static object?[] ToObjectArray<T>(this IEnumerable<FeaturesForExercise> values, Func<FeaturesForExercise, T> selector) =>
            values.Select(selector).Select(o => (object?)o).ToArray();

        public static decimal GetMedian(this IEnumerable<decimal> values) => (decimal)values.Order().Select(o => (double)o).GetMedian();

        public static double GetMedian(this IEnumerable<double> values)
        {
            if (values is not IOrderedEnumerable<double>)
                values = values.Order().ToArray();
            var enumerable = values as double[] ?? values.ToArray();
            var count = enumerable.Count();
            if (count == 0)
                return 0;
            return enumerable[count / 2];
        }

        public static decimal GetStandardDeviation(this IEnumerable<decimal> values) => (decimal)values.Select(o => (double)o).GetStandardDeviation();

        public static double GetStandardDeviation(this IEnumerable<double> values)
        {
            var enumerable = values as double[] ?? values.ToArray();
            var count = enumerable.Count();
            if (count > 1)
            {
                var avg = enumerable.Average();
                var sum = enumerable.Sum(d => (d - avg) * (d - avg));
                return Math.Sqrt((sum / count));
            }
            return 0;
        }
    }
}
