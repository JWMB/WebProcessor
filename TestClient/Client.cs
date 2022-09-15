using ProblemSource.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestClient
{
    internal class Client
    {
        private readonly SyncService syncService;
        private readonly string uuid;

        private readonly List<LogItem> logItems = new();
        private int trainingDay;

        public Client(SyncService syncService, string uuid)
        {
            this.syncService = syncService;
            this.uuid = uuid;
        }

        public async Task Init()
        {
            var data = new SyncInput
            {
                ApiKey = "abc",
                Uuid = uuid,
                RequestState = true,
            };
            await Sync(data);
        }

        private async Task Push()
        {
            var data = new SyncInput
            {
                ApiKey = "abc",
                Uuid = uuid,
                RequestState = false,
                Events = logItems.ToArray()
            };
            await Sync(data);
            Console.WriteLine($"Synced {uuid}, {logItems.Count} items");
            logItems.Clear();
        }

        private async Task Sync(SyncInput data)
        {
            var response = await syncService.Post(data);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"{response.StatusCode} {response.ReasonPhrase}");
            }
            var responseData = await response.Content.ReadAsStringAsync();
            var syncResult = Newtonsoft.Json.JsonConvert.DeserializeObject<SyncResult>(responseData);
            if (syncResult == null)
                throw new NullReferenceException("SyncResult NULL");
            var state = Newtonsoft.Json.JsonConvert.DeserializeObject<UserFullState>(syncResult.state);
            //fullState["exercise_stats"] = d.exercise_stats;
            //fullState["user_data"] = d.user_data;
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

            await Push();
        }
    }

    public static class DateTimeExtensions
    {
        public static long UnixTimestamp(this DateTime dateTime) => (long)(dateTime - new DateTime(1970, 1, 1)).TotalMilliseconds;
    }
}
