using ProblemSource.Models;
using ProblemSource.Models.LogItems;
using System.Net.Http.Json;

namespace TestClient
{
    internal class Client
    {
        private readonly SyncService syncService;
        private readonly string uuid;

        private readonly List<LogItem> logItems = new();
        private int trainingDay;
        private int phaseNumInDay;

        public Client(SyncService syncService, string uuid)
        {
            this.syncService = syncService;
            this.uuid = uuid;
        }

        public async Task Init()
        {
            var data = new SyncInput
            {
                Uuid = uuid,
                RequestState = true,
            };
            await Sync(data);
        }

        public async Task Sync()
        {
            var data = new SyncInput
            {
                Uuid = uuid,
                RequestState = false,
                Events = logItems.ToArray()
            };
            await Sync(data);
            //Console.WriteLine($"Synced {uuid}, {logItems.Count} items");
            logItems.Clear();
        }

        public async Task Sync(SyncInput data)
        {
            if (data.Events?.Any() == true)
            {
                foreach (var item in data.Events)
                {
                    if (item is LogItem logItem)
                    {
                        logItem.className = logItem.GetType().Name;
                    }
                }
            }
            var response = await syncService.Post(data);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"{response.StatusCode} {response.ReasonPhrase}");
            }
            var syncResult = await response.Content.ReadFromJsonAsync<SyncResult>();
            //var responseData = await response.Content.ReadAsStringAsync();
            //var syncResult = Newtonsoft.Json.JsonConvert.DeserializeObject<SyncResult>(responseData);
            if (syncResult == null)
                throw new NullReferenceException($"{uuid}: SyncResult NULL");
            if (!string.IsNullOrEmpty(syncResult.error))
                throw new NullReferenceException($"{uuid}: {syncResult.error}");

            var state = Newtonsoft.Json.JsonConvert.DeserializeObject<UserFullState>(syncResult.state);
            //fullState["exercise_stats"] = d.exercise_stats;
            //fullState["user_data"] = d.user_data;
        }

        public void GenerateNextPhase()
        {
            phaseNumInDay++;

            logItems.Add(new NewPhaseLogItem
            {
                time = DateTime.Now.UnixTimestamp(),
                exercise = "ex1",
                training_day = trainingDay,
                sequence = 0,
            });

            for (int i = 0; i < 10; i++)
            {
                logItems.Add(new NewProblemLogItem
                {
                    time = DateTime.Now.UnixTimestamp(),
                    level = 1.5M,
                    problem_string = "1+2",
                    problem_type = "N/A",
                });

                logItems.Add(new AnswerLogItem
                {
                    time = DateTime.Now.UnixTimestamp(),
                    answer = "3"
                });
            }

            logItems.Add(new PhaseEndLogItem
            {
                time = DateTime.Now.UnixTimestamp(),
                wonRace = true,
            });

            if (phaseNumInDay == 10)
            {
                //logItems.Add(new EndOfDayLogItem { training_day = trainingDay });
                trainingDay++;
                phaseNumInDay = 0;
            }
        }

        public async Task PlayDay(int trainingDay)
        {
            await Init();

            await Task.Delay(100);

            this.trainingDay = trainingDay;
            for (int i = 0; i < 10; i++)
            {
                await PlayPhase();

                await Task.Delay(1000);
            }
        }

        private async Task PlayPhase()
        {
            logItems.Add(new NewPhaseLogItem
            {
                time = DateTime.Now.UnixTimestamp(),
                exercise = "ex1",
                training_day = trainingDay,
                sequence = 0,
            });

            await Task.Delay(1000);

            for (int i = 0; i < 10; i++)
            {
                await PlayProblem();
            }

            await Task.Delay(1000);

            logItems.Add(new PhaseEndLogItem
            {
                time = DateTime.Now.UnixTimestamp(),
                wonRace = true,
            });
        }

        private async Task PlayProblem()
        {
            logItems.Add(new NewProblemLogItem
            {
                time = DateTime.Now.UnixTimestamp(),
                level = 1.5M,
                problem_string = "1+2",
                problem_type = "N/A",
            });
            
            await Task.Delay(1000);

            logItems.Add(new AnswerLogItem
            {
                time = DateTime.Now.UnixTimestamp(),
                answer = "3"
            });

            await Sync();
        }
    }

    public static class DateTimeExtensions
    {
        public static long UnixTimestamp(this DateTime dateTime) => (long)(dateTime - new DateTime(1970, 1, 1)).TotalMilliseconds;
    }
}
