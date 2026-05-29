using Common;
using CsvHelper;
using ProblemSource.Models;
using ProblemSource.Models.Aggregates;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Models;
using ProblemSourceModule.Services.Storage;

namespace ProblemSourceModule.Services.TrainingAnalyzers
{
	public class AiCoachAnalyzer : ITrainingAnalyzer
	{
		private readonly IUserGeneratedDataRepositoryProviderFactory userDataProviderFactory;
		private readonly ITrainingRepository trainingRepository;
		private readonly IHttpClientFactory httpClientFactory;

		public AiCoachAnalyzer(IUserGeneratedDataRepositoryProviderFactory userDataProviderFactory, ITrainingRepository trainingRepository, IHttpClientFactory httpClientFactory)
		{
			this.userDataProviderFactory = userDataProviderFactory;
			this.trainingRepository = trainingRepository;
			this.httpClientFactory = httpClientFactory;
		}

		public static List<T> ReadAnonymous<T>(T instance, string data, bool hasHeader = true, string delimiter = "\t")
		{
			using var reader = new StringReader(data);
			using var csvReader = new CsvReader(reader, new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = hasHeader,
				Delimiter = delimiter,
				MissingFieldFound = args => { }
			});
			return csvReader.GetRecords(instance).ToList();
		}
		public static List<T> Read<T>(string data, bool hasHeader = true, string delimiter = "\t")
		{
			using var reader = new StringReader(data);
			using var csvReader = new CsvReader(reader, new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = hasHeader,
				Delimiter = delimiter,
				MissingFieldFound = args => { }
			});
			return csvReader.GetRecords<T>().ToList();
		}

		public async Task<string> GetResource(Uri template)
		{
			var client = httpClientFactory.CreateClient();
			var result = await client.GetAsync(template);
			result.EnsureSuccessStatusCode();
			return await result.Content.ReadAsStringAsync();
		}

		public async Task<string> CreatePrompt(string template, Dictionary<string, object> replacements)
		{
			var rendered = new Replacer().Execute(template, replacements, null, [
				("MarkdownTable", (object items) => ToMarkdownTable(items)),
				]);
			return rendered;
		}

		public async Task<string> CreatePrompt(Dictionary<string, object> replacements)
		{
			var path = Path.Join(Directory.GetCurrentDirectory(), "Resources", "AICoach");
			
			var template = File.ReadAllText(Path.Join(path, "TeacherStudent.txt"));
			var exercises = File.ReadAllText(Path.Join(path, "Exercises.txt"));
			replacements["exerciseDescriptions"] = ReadAnonymous(new { id = "", type = "", description = "" }, exercises);
			var rendered = new Replacer().Execute(template, replacements, null, [
					("MarkdownTable", (object items) => ToMarkdownTable(items)),
				]);
			return rendered;
		}

		public static string ListToMarkdownTable(List<List<string>> table)
			=> string.Join("\n", new[] { "-------", string.Join("\n", table.Select(o => string.Join("\t", o))), "--------" });

		public static string ToMarkdownTable(object items)
		{
			if (items is System.Collections.IEnumerable objs)
			{
				var e = objs.GetEnumerator();

				object? first = null;
				while (e.MoveNext())
				{
					if (e.Current == null)
						continue;
					first = e.Current;
					break;
				}

				if (first == null)
					return "";

				if (first is System.Collections.IDictionary idi)
				{
				}
				else if (first is System.Collections.IList ili)
				{
					var item1 = ili[0];
					if (item1 != null)
					{
						var list = new List<List<string>>();
						e = objs.GetEnumerator();
						while (e.MoveNext())
						{
							if (e.Current == null)
								continue;
							var t = e.Current as IEnumerable<object>;
							if (t != null)
								list.Add(t.Select(o => $"{o}").ToList());
						}
						return ListToMarkdownTable(list);
					}
				}
				else
				{
					var props = first.GetType().GetProperties().Where(o => o.CanRead).ToList();
					var table = new List<List<string>>();
					table.Add(props.Select(o => o.Name).ToList());
					foreach (var obj in objs)
					{
						if (obj == null)
							table.Add(new List<string>());
						table.Add(obj == null ? new() : props.Select(o => ToString(o.GetValue(obj))).ToList());
					}
					return ListToMarkdownTable(table);
				}
			}
			return "...";
		}

		private static string ToString(object? obj)
		{
			if (obj == null) return "";
			if (obj is decimal d) return d.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
			if (obj is float f) return f.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
			if (obj is double db) return db.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
			return obj.ToString() ?? "";
		}

		public async Task<Dictionary<string, object>> CreateReplacements(int trainingId)
		{
			var training = await trainingRepository.Get(trainingId);
			if (training == null)
				throw new ArgumentException();

			var normedProviders = new List<(Training, IUserGeneratedDataRepositoryProvider)>();
			var norms = new[] { $"norm_{training.AgeBracket}", $"norm_{training.AgeBracket}_stddev" };
			foreach (var n in norms)
			{
				var t = await trainingRepository.GetByUsername(n);
				if (t != null)
					normedProviders.Add((t, userDataProviderFactory.Create(t.Id)));
			}
			return await CreateReplacements(training, userDataProviderFactory.Create(trainingId), normedProviders);
		}

		public async Task<Dictionary<string, object>> CreateReplacements(Training training, IUserGeneratedDataRepositoryProvider provider,
			IEnumerable<(Training Training, IUserGeneratedDataRepositoryProvider Provider)> referenceTrainingProviders, int? cutoffDay = null)
		{
			// TODO: data from... where?
			var generalPlan = """
				* 5 sessions per week, except holiday weeks
				""".Trim();
			// 	* Thursday sessions will be in a noisy setting

			var audience = $"a parent of the trainee (who is {training.AgeBracket} years old)"; // $"the trainee, {training.AgeBracket} years old";

			var earlierCoachingSessions = new[]
			{
				new { Date = DateTimeOffset.UtcNow.AddDays(-5), Notes = "" }
			}.ToList();
			earlierCoachingSessions.Clear();

			var plannedSessions = new[] { new { Date = DateTime.Today } }.ToList(); // TODO: option for user to provide a plan / schedule so we can evaluate adherence
			plannedSessions.Clear();

			var userIsFreeToChooseExercises = false; // TODO: from some training.Settings.

			// TODO: variability in response times, min level as well as max

			var baseLineIsFirstNMinutes = 5;

			var normProviders = referenceTrainingProviders.Where(o => o.Training.AgeBracket == training.AgeBracket);
			var normProvider = normProviders.FirstOrDefault(o => o.Training.Username.EndsWith(o.Training.AgeBracket)).Provider;
			var stdDevProvider = normProviders.FirstOrDefault(o => o.Training.Username.EndsWith("_stddev")).Provider;

			var allDays = (await provider.TrainingDays.GetAll()).OrderBy(o => o.TrainingDay).ToList();
			var today = DateTime.Today;
			if (cutoffDay.HasValue)
			{
				allDays = allDays.Where(o => o.TrainingDay <= cutoffDay.Value).ToList();
				today = allDays.MaxBy(o => o.TrainingDay)?.StartTime.Date ?? DateTime.Today;
			}
			{
				// TODO: temp
				var tmt = allDays.TakeLast(5).FirstOrDefault()?.StartTime;
				if (tmt != null)
					earlierCoachingSessions.Add(new { Date = new DateTimeOffset(tmt.Value), Notes = "" });
			}

			if (earlierCoachingSessions.Any())
			{
				var detailedInformationFor = allDays.Where(o => o.StartTime >= earlierCoachingSessions.Last().Date).ToList();
				detailedInformationFor = detailedInformationFor.TakeLast(7).ToList(); // limit to last 7 days (lots of data)
				var trainingDays = detailedInformationFor.Select(o => o.TrainingDay).ToList();
				var phases = (await provider.Phases.GetAll()).Where(o => trainingDays.Contains(o.training_day)).ToList();
				// TODO: slow - fetch for relevant days only
				var phaseDetails = phases.OrderBy(o => o.time).SelectMany(phase =>
				{
					// TODO: average / stddev per phase for correct vs incorrect
					// maybe: extra pauses after incorrect answers? higher likelihood of another incorrect answer after a first one?
					// but for some exercises (e.g. WM), level highly affects response time and accuracy, so need to control for that as well.

					var probsWithAnswers = phase.problems.Where(o => o.answers.Any()).ToList();
					var stats = probsWithAnswers.Select((prob, index) =>
						{
							// for response times, only consider first answer.
							var lastEnd = index == 0 ? null : probsWithAnswers[index - 1].answers.LastOrDefault()?.time;
							return new
							{
								Exercise = phase.exercise,
								Start = phase.time,
								Day = phase.training_day,
								Level = prob.level,
								PreviousLevel = index == 0 ? null : (decimal?)probsWithAnswers[index - 1].level,
								TimeSinceLast = lastEnd == null ? null : (long?)(prob.time - lastEnd.Value),
								ResponseTime = prob.answers.First().response_time,
								FirstCorrect = prob.answers.First().correct,
								NumIncorrect = prob.answers.Count(o => !o.correct),
								PreviousCorrect = index == 0 ? null : (bool?)probsWithAnswers[index - 1].answers.First().correct
							};
						}).ToList();
					//	ResponseTimes = new 
					//	{
					//		Correct = stats.Where(o => o.FirstCorrect == true).Select(o => o.ResponseTime).ToList(),
					//		Incorrect = stats.Where(o => o.FirstCorrect == false).Select(o => o.ResponseTime).ToList(),
					//		AfterCorrect = withLastCorrect.Where(o => o.LastCorrect != false).Select(o => o.Data.ResponseTime).ToList(),
					//		AfterIncorrect = withLastCorrect.Where(o => o.LastCorrect == false).Select(o => o.Data.ResponseTime).ToList(),
					//	},
					//	// Probability of failure after previous failure
					//};
					return stats;
				}).ToList();
				var timeForCorrectByExerciseAndLevel = phaseDetails
					.GroupBy(o => o.Exercise)
					.ToDictionary(
						o => o.Key,
						o => o.GroupBy(p => (int)p.Level)
						.ToDictionary(p => p.Key, p => p.Select(q => new { q.FirstCorrect, q.ResponseTime, q.PreviousLevel, q.PreviousCorrect }).ToList()));
			}

			var latestDay = allDays.Max(o => o.TrainingDay);
			var stats = (await provider.PhaseStatistics.GetAll()).OrderBy(o => o.training_day).ToList();
			if (cutoffDay.HasValue)
			{
				stats = stats.Where(o => o.training_day <= cutoffDay.Value).ToList();
			}

			var excludeExercises = new[] { "mathtest", "numbercomparison" };

			var normDays = normProvider == null ? [] : (await normProvider.TrainingDays.GetAll()).ToList();
			var normStats = normProvider == null ? [] : (await normProvider.PhaseStatistics.GetAll()).ToList();

			if (cutoffDay.HasValue)
			{
			}

			var timePerSession = allDays.Select(o => {
				var n = normDays.SingleOrDefault(p => p.TrainingDay == o.TrainingDay);
				return new
				{
					Date = o.StartTime.ToString("yyyy-MM-dd HH:mm"),
					Weekday = o.StartTime.ToString("dddd"),
					DurationMinutes = Math.Round((o.EndTimeStamp - o.StartTime).TotalMinutes),
					ExpectedMinutes = training.Settings.timeLimits.FirstOrDefault(33M),
					ActivePercentage = ActivePercentage(o),
					ActivePercentageAgeNorm = n == null ? "" : ActivePercentage(n),
					//AccuracyComparedToNorm = 1.1M
				};
				string ActivePercentage(TrainingDayAccount tda) => $"{Math.Round(100.0 * tda.ResponseMinutes / (tda.RemainingMinutes + tda.ResponseMinutes))}%";
			}).ToList();

			var phaseStatsByBracket = new Dictionary<string, List<PhaseStatistics>>();
			if (normProvider != null)
				phaseStatsByBracket.Add("norm", normStats);
			//foreach (var (bracket, prov) in referenceTrainingProviders)
			//	phaseStatsByBracket.Add(bracket, (await prov.PhaseStatistics.GetAll()).ToList());
			var baselineByExercise = stats.Select(o => ExerciseStats.getSharedId(o.exercise).ToLower()).Distinct()
				.Select(exerciseId =>
				{
					var usersBaseline = GetBaseline(stats, exerciseId, baseLineIsFirstNMinutes);
					var averages = phaseStatsByBracket.Select(p => new { Who = $"Norm", Value = GetBaseline(p.Value, exerciseId, baseLineIsFirstNMinutes) })
						.Concat([new { Who = "This user", Value = usersBaseline }]);
					return new { Exercise = exerciseId, Values = averages.ToList() };
				});
			var baselineByExerciseTable = new[] { new[] { "Exercise" }.Concat(baselineByExercise.First().Values.Select(o => o.Who)).ToList() }
				.Concat(baselineByExercise.Select(o =>
				{
					return new[] { o.Exercise }.Concat(o.Values.Select(p => ToString(p.Value))).ToList();
				})).ToList();

			var allExercises = stats.Concat(normStats == null ? [] : normStats.Where(o => o.training_day <= latestDay))
				.Select(o => ExerciseStats.getSharedId(o.exercise).ToLower()).Distinct().ToList();
			var maxLevelsWithNorm = Enumerable.Range(1, latestDay).Select(day =>
			{
				var byExercise = allExercises
					.Select(o => new { Exercise = o, User = GetMax(stats, o, day), Norm = normStats == null ? null : GetMax(normStats, o, day) })
					.Where(o => o.User != null || o.Norm != null)
					.ToList();
				return new { Day = day, ByExercise = byExercise.ToDictionary(o => o.Exercise, o => new { o.User, o.Norm }) };
			}).ToList();
			var usedExercises = maxLevelsWithNorm.SelectMany(o => o.ByExercise.Keys).Distinct().ToList();
			var maxLevelWithNormTable = new[] { new[] { "Day" }.Concat(usedExercises.SelectMany(o => new[] { "User", "Norm" }.Select(p => $"{o}:{p}"))).ToList() }
				.Concat(maxLevelsWithNorm.Select(o =>
			{
				var xx = usedExercises.SelectMany(p =>
				{
					var tmp = o.ByExercise.TryGetValue(p, out var v) ? v : null;
					return new[] { ToString(tmp?.User), ToString(tmp?.Norm) };
				});
				return new[] { o.Day.ToString() }.Concat(xx).ToList();
			})).ToList();

			var obj = new // Dictionary<string, object>
			{
				today = $"{today}:yyyy-MM-dd",
				training,
				plannedSessions,
				userIsFreeToChooseExercises,
				generalPlan,
				timePerSession,
				baseLineIsFirstNMinutes,
				baselineByExerciseTable,
				maxLevelWithNormTable,
				earlierCoachingSessions,
				audience
			};

			return obj.GetType().GetProperties()
				.Select(o => KeyValuePair.Create(o.Name, o.GetValue(obj)))
				.Where(o => o.Value != null)
				.ToDictionary(o => o.Key, o => o.Value!);

			decimal? GetMax(List<PhaseStatistics> stats, string exerciseId, int trainingDay)
			{
				var found = stats.Where(o => ExerciseStats.getSharedId(o.exercise).ToLower() == exerciseId && o.training_day == trainingDay);
				return found.Any() ? found.Max(o => o.level_max) : null;
			}

			decimal GetBaseline(List<PhaseStatistics> stats, string exerciseId, int firstMinutes)
			{
				var ordered = stats.Where(o => ExerciseStats.getSharedId(o.exercise).ToLower() == exerciseId).OrderBy(o => o.timestamp).ToList();
				var totalDuration = 0.0;
				var maxLevel = 0M;
				foreach (var item in ordered)
				{
					var duration = (item.end_timestamp - item.timestamp).TotalMinutes;
					totalDuration += duration;
					maxLevel = Math.Max(maxLevel, item.level_max);
					if (totalDuration >= firstMinutes)
						return maxLevel;
				}
				return maxLevel;
			}
		}

		public interface IGameAnalysis
		{
			decimal GetExpectedResponseFactor(Problem p, Answer a);
		}

		public static Dictionary<string, List<XX>> GetTrialAnalysis(IEnumerable<Phase> phases)
		{
			// per exercise: average response time per level
			var tmp = phases.GroupBy(o => o.exercise)
				.ToDictionary(
				byEx => byEx.Key,
				byEx => {
					var responseTimesFirstCorrect = byEx
						.SelectMany(phase =>
							phase.problems.Select(p => new { Level = p.level, Answer = p.answers.FirstOrDefault() }).Where(o => o.Answer?.correct == true))
						.GroupBy(o => (int)o.Level)
						.ToDictionary(
							o => o.Key,
							o => new {
								Avg = decimal.Round(o.Select(p => (decimal)p.Answer!.response_time).Average(), 2),
								SD = decimal.Round(o.Select(p => (decimal)p.Answer!.response_time).StdDev(), 2),
								Count = o.Count()
							});

					var tmp = byEx.SelectMany(phase => phase.problems
					.Select(p => {
						var first = p.answers.FirstOrDefault();
						if (first == null)
							return null;
						var last = p.answers.Last();
						return
						new XX
						{
							Day = phase.training_day,
							Level = p.level,
							Correct = first.correct,
							ResponseTime = first.response_time,
							Tries = first.tries,
							PhaseTime = p.time,
							Time = first.time,
							LastTime = last.time,
							LastResponseTime = last.response_time,
							AnswerCount = p.answers.Count
						};
					})).Where(o => o != null).ToList(); //))).ToList();
					return tmp.OfType<XX>().OrderBy(o => o.Day).ThenBy(o => o.Time).ToList();
				});

			var statsByGame = tmp.ToDictionary(
				byEx => byEx.Key,
				byEx => byEx.Value.Skip(30).GroupBy(o => (int)o.Level)
				.ToDictionary(
					o => o.Key,
					o => {
						var xx = o.ToList();
						var poa = xx.Select((x, i) =>
						{
							var lastTrial = i > 0 ? xx[i - 1] : null;
							return new {
								TD = lastTrial == null ? 0 : x.Time - lastTrial.Time,
								Last = lastTrial == null ? 0 : lastTrial.ResponseTime,
								TimeDiff = lastTrial == null ? 0 : x.PhaseTime - lastTrial.PhaseTime - lastTrial.ResponseTime,
								TimeDiffX = lastTrial == null ? 0 : x.Time - lastTrial.Time - lastTrial.ResponseTime,
								RespTime = x.ResponseTime,
								PDiff = lastTrial == null ? 0 : x.PhaseTime - lastTrial.PhaseTime
							};
						}).ToList();
						// avg time on incorrect doesn't make sense for WM since input is aborted after first mistake - we'd need to know # input items
						var correct = o.Where(p => p.Correct).ToList();
						return new
						{
							Avg = correct.Any() == false ? 0 :double.Round(correct.Select(p => p.ResponseTime).Average(), 2),
							Count = correct.Count,
							StdDev = correct.Any() == false ? 0 : decimal.Round(correct.Select(p => (decimal)p.ResponseTime).StdDev(), 2),
						};
					})
			);

			var aaa = tmp.Select(kv =>
			{
				var statsByLevel = statsByGame[kv.Key];

				foreach (var trial in kv.Value)
				{
					var stats = statsByLevel[(int)trial.Level];
					var diff = trial.ResponseTime - stats.Avg;
				}
				return 0;
			});

			return tmp;
		}

		public class XX
		{
			public int Day { get; set; }
			public decimal Level { get; set; }
			public bool Correct { get; set; }
			public int ResponseTime { get; set; }
			public int Tries { get; set; }
			public long Time { get; set; }
			public long PhaseTime { get; set; }
			public int LastResponseTime { get; set; }
			public long LastTime { get; set; }
			public int AnswerCount { get; set; }
			public override string ToString() => $"{Level} {Correct} {ResponseTime} {Tries}";
		}

		public async Task<bool> Analyze(Training training, IUserGeneratedDataRepositoryProvider provider, List<LogItem>? latestLogItems)
		{
			throw new NotImplementedException();
		}
	}
}
