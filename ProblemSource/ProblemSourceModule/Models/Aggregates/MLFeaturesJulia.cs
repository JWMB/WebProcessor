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

        public static MLFeaturesJulia FromPhases(TrainingSettings trainingSettings, IEnumerable<Phase> phases, int age, List<ExerciseGlobals>? exerciseGlobals = null, int dayCutoff = 5)
        {
            return new MLFeaturesJulia
            {
                ByExercise = phases
                    .Where(o => o.training_day <= dayCutoff)
                    .Where(o => o.phase_type != "GUIDE")
                    .Where(o => !o.exercise.EndsWith("#intro"))
                    .Select(o => new { Name = Phase.GetExerciseCommonName(o.exercise).ToLower(), Phase = o })
                    .GroupBy(o => o.Name)
                    //.GroupBy(o => Phase.GetExerciseCommonName(o.exercise).ToLower())
                    .ToDictionary(o => o.Key, o => 
                        FeaturesForExercise.Create(o.Select(p => p.Phase), exerciseGlobals == null ? new ExerciseGlobals() : exerciseGlobals.Single(p => p.Exercise == o.Key))),
                MeanTimeIncrease = 0,
                TrainingTime20Min = trainingSettings.timeLimits.FirstOrDefault() == 20M,
                Age6_7 = age == 6,
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
                    .ToObjectArray(o => o.FractionCorrect),

                // Note: tangram instead of wmGrid
                new[] { npals, tangram, numberline, mathTest01, nvr_rp, nvr_so, numberComparison01 }
                    .ToObjectArray(o => o.NumProblemsWithAnswers),

                new[] { wmGrid, npals, numberline, rotation, nvr_rp, mathTest01, numberComparison01 }
                    .ToObjectArray(o => o.StandardDeviation),

                new[] { npals, numberline, nvr_so, nvr_rp }
                    .ToObjectArray(o => o.HighestLevelInt),

                // all_data[nr_exercises] = all_data[nr_exercises] / all_data[highest_level]
                new[] { npals, tangram, numberline, rotation, nvr_rp }
                    .ToObjectArray(o => o.NumProblemsToHighestLevelDivHighestLevel), // NumProblemsDivHighestLevel NumExercisesToHighestLevel

                new[]{ (object)MeanTimeIncrease },

                // Note: description for NVR SO slightly different - "time correct" instead of "median time correct"
                new[] { tangram, rotation, nvr_so, mathTest01, numberComparison01 }
                    .ToObjectArray(o => o.MedianTimeCorrect),

                //new[] { wmGrid, npals, rotation, mathTest01 }
                //    .ToObjectArray(o => o.MedianTimeIncorrect),
                new[] { wmGrid, npals, rotation, mathTest01 }
                    .ToObjectArray(o => o.MedianTimeCorrectSubIncorrect),

                new[] { npals, rotation, numberline, nvr_rp, nvr_so, numberComparison01 }
                    .ToObjectArray(o => o.NumHighResponseTimes),

                new[] { mathTest01, npals, nvr_rp, nvr_so, rotation, tangram }
                    .ToObjectArray(o => o.Skew),

                new[] { npals, numberline, nvr_rp }
                    .ToObjectArray(o => o.MedianLevel),

                new[]{ (object)(TrainingTime20Min ? 1 : 0) },

                new[]{ (object)(Age6_7 ? 1 : 0) },

            }.SelectMany(o => o).Select(o => o?.ToString() ?? "").ToArray();

            FeaturesForExercise GetFeatures(string exercise) => ByExercise.GetValueOrDefault(exercise.ToLower(), new FeaturesForExercise());
        }

        public static List<ExerciseGlobals> ReadExerciseGlobals()
        {
            var str = File.ReadAllText(@"C:\Users\uzk446\Desktop\JuliaData\globals.csv");
            var result = str.Trim().Split('\n').Skip(1).Select(o => {
                var items = o.Trim().Split(',');
                return new ExerciseGlobals { Exercise = CorrectExercise(items[0]), Median = double.Parse(items[1]), StdDev = double.Parse(items[2]) };
            }).ToList();
            return result;

            string CorrectExercise(string input)
            {
                var val = input.ToLower().Replace(" ", "_");
                if (val.StartsWith("math_test") || val.StartsWith("number_comparison")) return val.Replace("_", "");
                return val;
            }
        }

        public class ExerciseGlobals
        {
            public string Exercise { get; set; } = "";
            public double Median { get; set; }
            public double StdDev { get; set; }
        }

        public class FeaturesForExercise
        {
            public int NumProblems { get; set; }

            public decimal FractionCorrect { get; set; }
            public int NumProblemsWithAnswers { get; set; }
            public decimal StandardDeviation { get; set; }
            public int HighestLevelInt { get; set; }
            /// <summary>
            /// 0-indexed!
            /// </summary>
            public int NumProblemsToHighestLevel { get; set; }
            public decimal? NumProblemsDivHighestLevel => HighestLevelInt == 0 ? null : 1M * NumProblems / HighestLevelInt;
            public decimal? NumProblemsToHighestLevelDivHighestLevel => HighestLevelInt == 0 ? null : 1M * NumProblemsToHighestLevel / HighestLevelInt;


            public int MedianTimeCorrect { get; set; }
            public int MedianTimeIncorrect { get; set; }
            public int MedianTimeCorrectSubIncorrect => MedianTimeCorrect - MedianTimeIncorrect;

            public int NumHighResponseTimes { get; set; }

            public int Skew { get; set; }

            public decimal MedianLevel { get; set; }

            private class ResponseTimesStats
            {
                public decimal MinLevel { get; set; }
                public decimal MaxLevel { get; set; }

                /// <summary>
                /// May be replace with the global (level-independent) cutoff value
                /// </summary>
                public double OutlierCutOff { get; set; }

                public bool IsOutlier(double responseTime) => responseTime > OutlierCutOff;
                public bool IsNotOutlier(double responseTime) => responseTime <= OutlierCutOff;

                public List<double> ResponseTimes { get; set; } = new();

                public double Mean => ResponseTimes.Average();
                public double MeanNoOutliers => ResponseTimesNoOutliers.Average();

                public IEnumerable<double> ResponseTimesNoOutliers => ResponseTimes.Where(IsNotOutlier);

                public double StandardDeviationNoOutliers => ResponseTimesNoOutliers.StdDev();

                public static ResponseTimesStats Calc(IEnumerable<Problem> problems)
                {
                    var result = new ResponseTimesStats();

                    result.MaxLevel = problems.Max(o => o.level);
                    result.MinLevel = problems.Min(o => o.level);

                    result.ResponseTimes = problems.SelectMany(p => p.answers.Select(a => (double)a.response_time)).ToList();
                    var ceiling = 0; // 60 * 1000;
                    if (ceiling > 0)
                        result.ResponseTimes = result.ResponseTimes.Select(o => Math.Min(o, ceiling)).ToList();

                    var lnResponseTimes = result.ResponseTimes.Select(t => Math.Log(t));
                    result.SetCutoff(lnResponseTimes.Median(), lnResponseTimes.StdDev());

                    return result;
                }

                public void SetCutoff(double lnMedian, double lnStdDev)
                {
                    // ln(RT) > median(ln(all RTs)) + 2.5 * sd(ln(all RTs))
                    // Add small value for double precision purposes (if all have same value, they might be consider outliers)
                    OutlierCutOff = 0.001 + Math.Exp(lnMedian + 2.5 * lnStdDev);
                }
            }

            public static FeaturesForExercise Create(IEnumerable<Phase> phases, ExerciseGlobals globals)
            {
                var stats = new FeaturesForExercise();

                // Calc pre-requisites:
                var allProblems = phases.SelectMany(phase => phase.problems).ToList();
                if (allProblems.Count == 0)
                    return stats;

                var responseTimesTotal = ResponseTimesStats.Calc(allProblems);
                if (responseTimesTotal.ResponseTimesNoOutliers.Any() == false)
                    return stats;


                var debugExercise = phases.First().exercise;

                var responseTimesPerLevel = allProblems.GroupBy(problem => (int)problem.level).ToDictionary(o => o.Key, ResponseTimesStats.Calc);

                //// NOTE: after discussions, we should NOT use level-specific cutoffs
                //foreach (var kv in responseTimesPerLevel)
                //    kv.Value.OutlierCutOff = responseTimesTotal.OutlierCutOff;

                responseTimesTotal.SetCutoff(globals.Median, globals.StdDev);
                foreach (var kv in responseTimesPerLevel)
                    kv.Value.SetCutoff(globals.Median, globals.StdDev);

                //var debug = responseTimesPerLevel.Select(o => new
                //{
                //    Level = o.Value.MinLevel,
                //    o.Value.OutlierCutOff,
                //    o.Value.Mean,
                //    o.Value.MeanNoOutliers,
                //    o.Value.StandardDeviationNoOutliers,
                //    Num = o.Value.ResponseTimes.Count(),
                //    NumOutliers = (o.Value.ResponseTimes.Count() - o.Value.ResponseTimesNoOutliers.Count())
                //});

                // Calc outputs:

                stats.NumProblemsWithAnswers = allProblems.Count(problem => problem.answers.Any());
                stats.FractionCorrect = 1M * allProblems.Count(problem => problem.answers.Any(answer => answer.correct)) / allProblems.Count();

                // Highest level reached (with at least one correct answered on that level)
                stats.HighestLevelInt = allProblems
                    .Where(HasCorrectAnswer)
                    .Max(problem => (int)problem.level);

                // Number of exercises: The number of exercises it took to reach the highest level defined above
                // "Exercise" here means combination of training_day, exercise and level
                // TODO: just reach level, or with correct answer?
                // Note: 0-indexed
                stats.NumProblemsToHighestLevel = phases.SelectMany(o => o.problems).ToList().FindIndex(o => (int)o.level == stats.HighestLevelInt);
                //stats.NumProblemsToHighestLevel = phases.SelectMany(o => o.problems).ToList().FindIndex(o => (int)o.level == stats.HighestLevelInt && o.answers.Any(a => a.correct));
                //var orderedPhases = phases.OrderBy(p => $"{p.training_day.ToString().PadLeft(3, '0')}_{p.time}").ToList();
                //stats.NumProblemsToHighestLevel = orderedPhases.FindIndex(phase => phase.problems.Any(p => (int)p.level == stats.HighestLevelInt));

                stats.NumProblems = phases.Sum(o => o.problems.Count());

                // 7) Median time correct: The median response time for correctly answered questions after outliers have been removed
                stats.MedianTimeCorrect = (int)allProblems
                    .Where(HasCorrectAnswer)
                    .Select(o => new { Level = (int)o.level, ResponseTime = (double)o.answers.First().response_time })
                    .Where(o => responseTimesPerLevel[o.Level].IsNotOutlier(o.ResponseTime))
                    .Select(o => o.ResponseTime)
                    .Median();

                // 8) Median time incorrect: The median response time for correctly answered questions minus the median response time for incorrectly answered questions after outliers have been removed
                stats.MedianTimeIncorrect = (int)allProblems
                    .Where(HasNoCorrectAnswer)
                    .Select(o => new { Level = (int)o.level, ResponseTime = (double)o.answers.First().response_time })
                    .Where(o => responseTimesPerLevel[o.Level].IsNotOutlier(o.ResponseTime))
                    .Select(o => o.ResponseTime)
                    .Median();
                //TODO: not sure this is what is expected

                // Std(tot) = (std(1)*3/10 + std(2)*2/10 + std(3)*5/10) / median_time_correct(tot)
                stats.StandardDeviation = (decimal)responseTimesPerLevel.Values
                    .Select(o => o.StandardDeviationNoOutliers * o.ResponseTimesNoOutliers.Count() / responseTimesTotal.ResponseTimesNoOutliers.Count())
                    .Sum()
                    / stats.MedianTimeCorrect;
                //var totalResponsesNoOutliersCount = responseTimesTotal.ResponseTimesNoOutliers.Count();
                //stats.StandardDeviation = (decimal)responseTimesPerLevel.Values
                //    .Select(o => {
                //        var timesInLevelNoOutlisers = o.ResponseTimes.Where(t => t < responseTimesTotal.OutlierCutOff).ToList();
                //        return timesInLevelNoOutlisers.StdDev() * timesInLevelNoOutlisers.Count() / totalResponsesNoOutliersCount;
                //    }).Sum() / stats.MedianTimeCorrect;


                // Std(tot) = Avg(std(level1)/mean_time(level1) + std(level2)/mean_time(level2) + …)
                //stats.StandardDeviation = (decimal)responseTimesPerLevel.Values
                //    .Select(o => o.StandardDeviationNoOutliers / o.MeanNoOutliers)
                //    .Average(); // TODO: was Sum().. Verify correct assumption to change this


                //9) Number of high response times: The number of questions with a response time above the outlier cutoff
                // stats.NumHighResponseTimes = allProblems.Count(problem => problem.answers.Any(answer => isOutlier(answer.response_time)));
                stats.NumHighResponseTimes = allProblems
                    .GroupBy(problem => (int)problem.level)
                    .Select(grp => {
                        var rt = responseTimesPerLevel[(int)grp.Key];
                        return grp.Count(problem => problem.answers.Any(answer => rt.IsOutlier(answer.response_time)));
                    }).Sum();

                //10) Skew: The skew for response times after outliers have been removed
                stats.Skew = 0; // TODO: ?

                //11) Median level: The median level of correctly answered questions
                stats.MedianLevel = allProblems.Where(problem => problem.answers.Any(answer => answer.correct))
                    .Select(problem => (decimal)(int)problem.level)
                    .Order()
                    .Median();

                return stats;

                bool HasCorrectAnswer(Problem problem) => problem.answers.Any(answer => answer.correct);
                bool HasNoCorrectAnswer(Problem problem) => HasCorrectAnswer(problem) == false;
            }
        }
    }

    public static class StatisticsExtensions
    {
        public static object?[] ToObjectArray<T>(this IEnumerable<FeaturesForExercise> values, Func<FeaturesForExercise, T> selector) =>
            values.Select(selector).Select(o => (object?)o).ToArray();

        public static decimal Median(this IEnumerable<decimal> values) => (decimal)values.Order().Select(o => (double)o).Median();

        public static double Median(this IEnumerable<double> values)
        {
            if (values is not IOrderedEnumerable<double>)
                values = values.Order().ToArray();
            var enumerable = values as double[] ?? values.ToArray();
            var count = enumerable.Count();
            if (count == 0)
                return 0;
            return enumerable[count / 2];
        }

        public static decimal StdDev(this IEnumerable<decimal> values) => (decimal)values.Select(o => (double)o).StdDev();

        public static double StdDev(this IEnumerable<double> values)
        {
            var enumerable = values as double[] ?? values.ToArray();
            var count = enumerable.Count();
            if (count <= 1)
                return 0;
            var avg = enumerable.Average();
            var sum = enumerable.Sum(d => (d - avg) * (d - avg));
            return Math.Sqrt(sum / (count - 1));
        }
    }
}
