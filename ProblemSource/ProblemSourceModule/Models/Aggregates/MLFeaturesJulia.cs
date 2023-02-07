using static ProblemSource.Models.Aggregates.MLFeaturesJulia;

namespace ProblemSource.Models.Aggregates
{
    public class ColumnTypeAttribute : Attribute
    {
        public enum ColumnType
        {
            Numeric,
            Text,
            Categorical,
            Ignored,
            ImagePath,
            Label,
            UserId
        }

        public ColumnType Type { get; private set; }

        public ColumnTypeAttribute(ColumnType type)
        {
            Type = type;
        }
    }

    public interface IMLFeature
    {
        Dictionary<string, object?> GetFlatFeatures();
        bool IsValid { get; }
    }

    public class MLFeaturesJulia : IMLFeature
    {
        public Dictionary<string, FeaturesForExercise> ByExercise { get; set; } = new();

        /// <summary>
        ///  Mean time increase: The difference in response time between the question following an incorrectly answered question
        ///  and the incorrectly answered question (response time after incorrect minus response time incorrect)
        /// </summary>
        public double MeanTimeIncrease { get; set; }

        /// <summary>
        /// 12) Training time 20 min: Dummy coded with a 1 if the training time is 20 min and 0 if the training time is 33 min per day.
        /// </summary>
        [ColumnType(ColumnTypeAttribute.ColumnType.Categorical)]
        public bool TrainingTime20Min { get; set; }

        /// <summary>
        /// 13) Age 6 - 7: Dummy coded with a 1 if the age is 6 - 7 and a 0 if the age is 7 - 8(other age groups have been excluded from the data set)
        /// </summary>
        [ColumnType(ColumnTypeAttribute.ColumnType.Categorical)]
        public bool Age6_7 { get; set; }

        [ColumnType(ColumnTypeAttribute.ColumnType.Ignored)]
        public int Age { get; set; }


        [ColumnType(ColumnTypeAttribute.ColumnType.Ignored)]
        public float? FinalNumberLineLevel { get; set; }

        [ColumnType(ColumnTypeAttribute.ColumnType.Label)]
        public int Outcome
        {
            get
            {
                //Spannen är:
                //0: < 24
                //1: <= 24 < 38
                //2: <= 38 < 48
                //3: <= 48 < 80
                //4: <= 80 < 95
                //5: >= 95
                return FinalNumberLineLevel switch
                {
                    //< 31 => 0,
                    //< 44 => 1,
                    //< 76 => 2,
                    //< 99 => 3,
                    //_ => 4,
                    < 24 => 0,
                    < 38 => 1,
                    < 48 => 2,
                    < 80 => 3,
                    < 95 => 4,
                    _ => 5
                };
            }
        }

        public bool IsValid =>
            FinalNumberLineLevel != null
            && ByExercise.ContainsKey("nvr_rp") && ByExercise["nvr_rp"].FractionCorrect.HasValue
            && ByExercise.ContainsKey("nvr_so") && ByExercise["nvr_so"].FractionCorrect.HasValue
            && (Age >= 6 && Age <= 7);

        public static List<int> ChunkLimits(IEnumerable<MLFeaturesJulia> features, int numChunks)
        {
            var finalLevels = features.Select(o => o.FinalNumberLineLevel).OfType<float>().Order().ToList();
            var chunkSize = finalLevels.Count / numChunks;
            var limits = finalLevels.Chunk(chunkSize).Take(numChunks).Select(o => o.Last());
            return limits.Select(o => (int)o).ToList();
        }

        [ColumnType(ColumnTypeAttribute.ColumnType.Ignored)]
        public int Id { get; set; }

        public static MLFeaturesJulia FromPhases(TrainingSettings trainingSettings, IEnumerable<Phase> phases, int age, List<ExerciseGlobals>? exerciseGlobals = null, int dayCutoff = 5)
        {
            exerciseGlobals ??= ExerciseGlobals.GetDefaults();

            var filtered = phases
                    .Where(o => o.training_day <= dayCutoff)
                    .Where(o => o.phase_type != "GUIDE")
                    .Where(o => !o.exercise.EndsWith("#intro"));
            //.GroupBy(o => Phase.GetExerciseCommonName(o.exercise).ToLower())
            var featuresByExercise = filtered
                    .Select(o => new { Name = Phase.GetExerciseCommonName(o.exercise).ToLower(), Phase = o })
                    .GroupBy(o => o.Name)
                    .ToDictionary(o => o.Key, o =>
                        FeaturesForExercise.Create(o.Select(p => p.Phase), exerciseGlobals.SingleOrDefault(p => p.Exercise == o.Key) ?? new ExerciseGlobals()));

            var levelNumberlineAroundDay35 = phases.Where(o => o.exercise.ToLower().StartsWith("numberline"))
                .Where(o => o.training_day >= 33 && o.training_day <= 37)
                .Where(o => o.problems.Any())
                .GroupBy(o => o.training_day)
                .Select(o => new { Day = o.Key, MaxLevel = o.Max(phase => phase.problems.Max(p => p.level)) })
                .OrderBy(o =>
                    {
                        var diff = o.Day - 35;
                        return diff < 0 ? -2 * diff : diff;
                    })
                .FirstOrDefault()?.MaxLevel ?? null;

            return new MLFeaturesJulia
            {
                ByExercise = featuresByExercise,
                MeanTimeIncrease = featuresByExercise.Values.Any() ? featuresByExercise.Values.Average(o => o.MeanTimeIncrease) : 0,
                TrainingTime20Min = trainingSettings.timeLimits.FirstOrDefault() == 20M,
                Age6_7 = age == 6,
                Age = age,
                FinalNumberLineLevel = (float?)levelNumberlineAroundDay35,
            };
        }
        
        public Dictionary<string, object?> GetFlatFeatures()
        {
            var npals = "npals";
            var wmGrid = "WM_grid";
            var numberline = "numberline";
            var mathTest01 = "mathTest01";
            var nvr_rp = "nvr_rp";
            var nvr_so = "nvr_so";
            var numberComparison01 = "numberComparison01";
            var tangram = "tangram";
            var rotation = "rotation";

            var root = new Dictionary<string, object?>();

            AddProperties(new[] { npals, wmGrid, numberline, mathTest01, nvr_rp, nvr_so, numberComparison01 },
                "FrCor", ffe => ffe.FractionCorrect);

            // Note: tangram instead of wmGrid
            AddProperties(new[] { npals, tangram, numberline, mathTest01, nvr_rp, nvr_so, numberComparison01 },
                "NumPrbWAns", ffe => ffe.NumProblemsWithAnswers);

            AddProperties(new[] { wmGrid, npals, numberline, rotation, nvr_rp, mathTest01, numberComparison01 },
                "SD", ffe => ffe.StandardDeviation);

            AddProperties(new[] { npals, numberline, nvr_so, nvr_rp },
                "MaxLvl", ffe => ffe.HighestLevelInt);

            // all_data[nr_exercises] = all_data[nr_exercises] / all_data[highest_level]
            AddProperties(new[] { npals, tangram, numberline, rotation, nvr_rp },
                "NumPrbToMaxLvlDivMaxLvl", ffe => ffe.NumProblemsToHighestLevelDivHighestLevel);

            root.Add("AvgTIncrease", MeanTimeIncrease);

            // Note: description for NVR SO slightly different - "time correct" instead of "median time correct"
            AddProperties(new[] { tangram, rotation, nvr_so, mathTest01, numberComparison01 },
                "MedTCorrect", ffe => ffe.MedianTimeCorrect);

            //new[] { wmGrid, npals, rotation, mathTest01 }
            //    .ToObjectArray(o => o.MedianTimeIncorrect),
            AddProperties(new[] { wmGrid, npals, rotation, mathTest01 },
                "MedTIncSubCor", ffe => ffe.MedianTimeIncorrectSubCorrect);

            AddProperties(new[] { npals, rotation, numberline, nvr_rp, nvr_so, numberComparison01 },
                "NumHighRT", ffe => ffe.NumHighResponseTimes);

            AddProperties(new[] { mathTest01, npals, nvr_rp, nvr_so, rotation, tangram },
                "Skew", ffe => ffe.Skew);

            AddProperties(new[] { npals, numberline, nvr_rp },
                "MedLvl", ffe => ffe.MedianLevel);

            // Need to have same name as properties so that the ColumnTypeAttribute values can be used for the JSON properties
            root.Add(nameof(TrainingTime20Min), TrainingTime20Min);
            root.Add(nameof(Age6_7), Age6_7);
            root.Add(nameof(FinalNumberLineLevel), FinalNumberLineLevel);
            root.Add(nameof(Outcome), Outcome);

            return root;

            void AddProperties(IEnumerable<string> exercises, string columnPrefix, Func<FeaturesForExercise, object?> func)
            {
                foreach (var exercise in exercises)
                    root!.Add($"{columnPrefix}_{exercise}", func(GetFeatures(exercise)));
            }
            FeaturesForExercise GetFeatures(string exercise) => ByExercise.GetValueOrDefault(exercise.ToLower(), new FeaturesForExercise());
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
                    .ToObjectArray(o => o.MedianTimeIncorrectSubCorrect),

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

        public class ExerciseGlobals
        {
            public string Exercise { get; set; } = "";
            public double Median { get; set; }
            public double StdDev { get; set; }

            public static List<ExerciseGlobals> GetDefaults()
            {
                return new[] {
                  new ExerciseGlobals {
                                    Exercise = "npals",
                    Median = 8.249,
                    StdDev = 0.62
                  },
                  new ExerciseGlobals {
                                    Exercise = "numberline",
                    Median = 8.47,
                    StdDev = 0.663
                  },
                  new ExerciseGlobals {
                                    Exercise = "mathtest01",
                    Median = 8.412,
                    StdDev = 0.717
                  },
                  new ExerciseGlobals {
                                    Exercise = "numbercomparison01",
                    Median = 6.95,
                    StdDev = 0.518
                  },
                  new ExerciseGlobals {
                                    Exercise = "wm_grid",
                    Median = 8.931,
                    StdDev = 0.317
                  },
                  new ExerciseGlobals {
                                    Exercise = "rotation",
                    Median = 7.865,
                    StdDev = 0.64
                  },
                  new ExerciseGlobals {
                                    Exercise = "tangram",
                    Median = 10.448,
                    StdDev = 0.544
                  },
                  new ExerciseGlobals {
                                    Exercise = "nvr_so",
                    Median = 8.483,
                    StdDev = 0.598
                  },
                  new ExerciseGlobals {
                                    Exercise = "nvr_rp",
                    Median = 8.868,
                    StdDev = 0.547
                  }
                }.ToList();
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
        }

        public class FeaturesForExercise
        {
            public int NumProblems { get; set; }

            public decimal? FractionCorrect { get; set; }
            public int NumProblemsWithAnswers { get; set; }
            public decimal? StandardDeviation { get; set; }
            public int? HighestLevelInt { get; set; }
            /// <summary>
            /// 0-indexed!
            /// </summary>
            public int? NumProblemsToHighestLevel { get; set; }
            public decimal? NumProblemsDivHighestLevel => HighestLevelInt == 0 ? null : 1M * NumProblems / HighestLevelInt;
            public decimal? NumProblemsToHighestLevelDivHighestLevel => HighestLevelInt == 0 ? null : 1M * NumProblemsToHighestLevel / HighestLevelInt;


            public int? MedianTimeCorrect { get; set; }
            public int? MedianTimeIncorrect { get; set; }
            public int? MedianTimeIncorrectSubCorrect => MedianTimeIncorrect.HasValue && MedianTimeCorrect.HasValue ? MedianTimeIncorrect - MedianTimeCorrect : null;

            public int? NumHighResponseTimes { get; set; }

            public double? Skew { get; set; }

            public decimal? MedianLevel { get; set; }

            /// <summary>
            ///  Mean time increase: The difference in response time between the question following an incorrectly answered question
            ///  and the incorrectly answered question (response time after incorrect minus response time incorrect)
            /// </summary>
            public double MeanTimeIncrease { get; set; }

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
                public List<double> ResponseTimesCorrect { get; set; } = new();

                public double Mean => ResponseTimes.Average();
                public double? MeanNoOutliers => ResponseTimesNoOutliers.Any() ? ResponseTimesNoOutliers.Average() : null;

                public IEnumerable<double> ResponseTimesNoOutliers => ResponseTimes.Where(IsNotOutlier);
                public IEnumerable<double> ResponseTimesCorrectNoOutliers => ResponseTimesCorrect.Where(IsNotOutlier);

                public double StandardDeviationCorrectNoOutliers => ResponseTimesCorrectNoOutliers.StdDev();

                public static ResponseTimesStats Calc(IEnumerable<Problem> problems)
                {
                    var result = new ResponseTimesStats();

                    result.MaxLevel = problems.Max(o => o.level);
                    result.MinLevel = problems.Min(o => o.level);

                    result.ResponseTimesCorrect = problems.SelectMany(p => p.answers.Where(a => a.correct).Select(a => (double)a.response_time)).ToList();
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
                // First remove answers with non-positive response_time and trim problems:
                foreach (var phase in phases)
                {
                    foreach (var problem in phase.problems)
                        problem.answers = problem.answers.Where(o => o.response_time > 0).ToList();
                    phase.problems = phase.problems.Where(o => o.answers.Any()).ToList();
                }

                // Calc pre-requisites:
                var allProblems = phases.SelectMany(phase => phase.problems).ToList();

                if (allProblems.Count == 0)
                    return stats;

                var responseTimesTotal = ResponseTimesStats.Calc(allProblems);
                if (responseTimesTotal.ResponseTimes.Any() == false)
                    return stats;

                // verify they come in correct order
                {
                    var prev = allProblems.First();
                    foreach (var p in allProblems.Skip(1))
                    {
                        var diff = p.time - prev.time;
                        if (diff < 0)
                        { }
                        prev = p;
                    }
                }

                var responseTimesPerLevel = allProblems.GroupBy(problem => (int)problem.level).ToDictionary(o => o.Key, ResponseTimesStats.Calc);

                var debugExercise = phases.First().exercise.ToLower();

                //// NOTE: after discussions, we should NOT use level-specific cutoffs
                //foreach (var kv in responseTimesPerLevel)
                //    kv.Value.OutlierCutOff = responseTimesTotal.OutlierCutOff;

                responseTimesTotal.SetCutoff(globals.Median, globals.StdDev);
                foreach (var kv in responseTimesPerLevel)
                    kv.Value.SetCutoff(globals.Median, globals.StdDev);

                var debugXX = Newtonsoft.Json.JsonConvert.SerializeObject(responseTimesPerLevel.Select(o => new 
                { o.Value.MinLevel, o.Value.OutlierCutOff, o.Value.Mean, o.Value.MeanNoOutliers, o.Value.StandardDeviationCorrectNoOutliers, o.Value.ResponseTimes.Count, ResponseTimesNoOutliersCount = o.Value.ResponseTimesNoOutliers.Count() }));

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
                var allProbsCorrectAnswer = allProblems
                    .Where(HasCorrectAnswer);
                stats.HighestLevelInt = allProbsCorrectAnswer.Any()
                    ? allProbsCorrectAnswer.Max(problem => (int)problem.level)
                    : null;

                // Number of exercises: The number of exercises it took to reach the highest level defined above
                // "Exercise" here means combination of training_day, exercise and level
                // TODO: just reach level, or with correct answer?
                // Note: 0-indexeds

                //stats.NumProblemsToHighestLevel = phases.SelectMany(o => o.problems).ToList().FindIndex(o => (int)o.level == stats.HighestLevelInt);
                stats.NumProblemsToHighestLevel = allProblems.FindIndex(o => (int)o.level == stats.HighestLevelInt && o.answers.Any(a => a.correct));
                //var orderedPhases = phases.OrderBy(p => $"{p.training_day.ToString().PadLeft(3, '0')}_{p.time}").ToList();
                //stats.NumProblemsToHighestLevel = orderedPhases.FindIndex(phase => phase.problems.Any(p => (int)p.level == stats.HighestLevelInt));
                // Note: in Julias example, outlier trials were filtered out
                //stats.NumProblemsToHighestLevel = allProblems.Where(o => o.answers.Any(a => responseTimesTotal.IsOutlier(a.response_time) == false)).ToList()
                    //.FindIndex(o => (int)o.level == stats.HighestLevelInt && o.answers.Any(a => a.correct));

                stats.NumProblems = phases.Sum(o => o.problems.Count());

                // 7) Median time correct: The median response time for correctly answered questions after outliers have been removed
                stats.MedianTimeCorrect = (int?)allProblems
                    .Where(HasCorrectAnswer)
                    .Select(o => new { Level = (int)o.level, ResponseTime = (double)o.answers.First().response_time })
                    .Where(o => responseTimesPerLevel[o.Level].IsNotOutlier(o.ResponseTime))
                    .Select(o => o.ResponseTime)
                    .MedianOrNull();

                // 8) Median time incorrect: The median response time for correctly answered questions minus the median response time for incorrectly answered questions after outliers have been removed
                stats.MedianTimeIncorrect = (int?)allProblems
                    .Where(HasNoCorrectAnswer)
                    .Select(o => new { Level = (int)o.level, ResponseTime = (double)o.answers.First().response_time })
                    .Where(o => responseTimesPerLevel[o.Level].IsNotOutlier(o.ResponseTime))
                    .Select(o => o.ResponseTime)
                    .MedianOrNull();
                //TODO: not sure this is what is expected

                // Std(tot) = (std(1)*3/10 + std(2)*2/10 + std(3)*5/10) / median_time_correct(tot)
                if (stats.MedianTimeCorrect > 0)
                    stats.StandardDeviation = (decimal)responseTimesPerLevel.Values
                        .Where(o => o.ResponseTimesCorrectNoOutliers.Any())
                        .Select(o => o.StandardDeviationCorrectNoOutliers * o.ResponseTimesCorrectNoOutliers.Count() / responseTimesTotal.ResponseTimesCorrectNoOutliers.Count())
                        .Sum()
                        / stats.MedianTimeCorrect.Value;
                // TODO: null if MedianTimeCorrect == 0?

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
                stats.Skew = responseTimesTotal.ResponseTimes.Any() ? responseTimesTotal.ResponseTimesCorrectNoOutliers.Skewness() : null;

                //11) Median level: The median level of correctly answered questions
                stats.MedianLevel = allProblems.Where(problem => problem.answers.Any(answer => answer.correct))
                    .Select(problem => (decimal)(int)problem.level)
                    .Order()
                    .MedianOrNull();

                var incorrectToNextRTDiffs = allProblems
                    .AggregateWithPrevious((p, c) => p.answers.Any(a => a.correct) == true ? null : (int?)(c.answers.First().response_time - p.answers.First().response_time))
                    .OfType<int>().ToList();
                stats.MeanTimeIncrease = incorrectToNextRTDiffs.Any() ? incorrectToNextRTDiffs.Average() : 0;

                return stats;

                bool HasCorrectAnswer(Problem problem) => problem.answers.Any(answer => answer.correct);
                bool HasNoCorrectAnswer(Problem problem) => HasCorrectAnswer(problem) == false;
            }
        }
    }

    public static class StatisticsExtensions
    {
        public static IEnumerable<TOut> AggregateWithPrevious<TIn, TOut>(this IEnumerable<TIn> values, Func<TIn, TIn, TOut> actOnPreviousAndCurrent)
        {
            var prev = values.First();
            foreach (var item in values.Skip(1))
            {
                yield return actOnPreviousAndCurrent(prev, item);
            }
        }

        public static object?[] ToObjectArray<T>(this IEnumerable<FeaturesForExercise> values, Func<FeaturesForExercise, T> selector) =>
            values.Select(selector).Select(o => (object?)o).ToArray();

        public static double? MedianOrNull(this IEnumerable<double> values) => values.Any() ? values.Median() : null;
        public static decimal? MedianOrNull(this IEnumerable<decimal> values) => values.Any() ? values.Median() : null;


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

        public static double? Skewness(this IEnumerable<double> values)
        {
            var enumerable = values as double[] ?? values.ToArray();

            if (values.Any() == false)
                return null;
            var avg = values.Average();
            var sd = values.StdDev();
            var cnt = (double)values.Count();

            var skewCum = 0.0d; // the cum part of SKEW formula
            for (int i = 0; i < enumerable.Length; i++)
            {
                var b = (enumerable[i] - avg) / sd;
                skewCum += b * b * b;
            }
            return cnt / (cnt - 1) / (cnt - 2) * skewCum;
            //var sum = 0d;
            //for (int i = 0; i < cnt; i++)
            //{
            //    var diff = enumerable[i] - values.Average();
            //    sum += diff * diff * diff;
            //}
            //return sum / (sd * sd * sd);
        }
    }
}
