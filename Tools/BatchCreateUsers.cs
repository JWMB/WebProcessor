using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ProblemSource.Models;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Models;
using ProblemSourceModule.Services;
using ProblemSourceModule.Services.Storage;
using System.Linq;
using System.Net.Mail;

namespace Tools
{
    public class BatchCreateUsers
    {
        private readonly IUserRepository userRepository;
        private readonly ITrainingRepository trainingRepository;
        private readonly ITrainingPlanRepository trainingPlanRepository;
        private readonly ITrainingUsernameService trainingUsernameService;
        private readonly Random rnd = new Random();
        public BatchCreateUsers(IUserRepository userRepository, ITrainingRepository trainingRepository, ITrainingPlanRepository trainingPlanRepository, ITrainingUsernameService trainingUsernameService)
        {
            this.userRepository = userRepository;
            this.trainingRepository = trainingRepository;
            this.trainingPlanRepository = trainingPlanRepository;
            this.trainingUsernameService = trainingUsernameService;
        }

        public async Task<IEnumerable<CreateUserResult>> CreateUsers(IEnumerable<string> emails, Dictionary<string, int>? groupsAndNumTrainings = null, string? trainingPlanName = null, bool actuallyCreate = false)
        {
            var result = new List<CreateUserResult>();
            foreach (var email in emails)
            {
                var user = await CreateUser(email, groupsAndNumTrainings, trainingPlanName: trainingPlanName, settings: null, actuallyCreate);
                result.Add(user);
            }
            return result;
        }

        public class CreateUserResult
        {
            public User User { get; set; } = new();
            public bool WasCreated { get; set; }
            public string Password { get; set; } = "";
            public Dictionary<string, List<string>> CreatedTrainings = new();

            public string CreatedTrainingsToString() => string.Join("\n", CreatedTrainings.Select(o => $"Group '{o.Key}':\n{string.Join("\n", o.Value.Select(n => $"* {n}"))}"));

            public override string ToString() => $"{User.Email}/{Password}: {CreatedTrainingsToString()}";
        }

        public async Task<CreateUserResult> CreateUser(string email, Dictionary<string, int>? groupsAndNumTrainings = null,
            string? trainingPlanName = null, TrainingSettings? settings = null, bool actuallyCreate = false)
        {
            var existing = await userRepository.Get(email);
            if (existing != null)
                return new CreateUserResult { User = existing, WasCreated = false };

            settings ??= new TrainingSettings { timeLimits = new List<decimal> { 33 } };

            var pwdChars = Enumerable.Range(48, 10)
                .Concat(Enumerable.Range(65, 25))
                .Concat(Enumerable.Range(97, 25))
                .Select(o => (char)o)
                .ToList();
            var password = string.Join("", Enumerable.Range(0, 6).Select(o => pwdChars[rnd.Next(pwdChars.Count)]));
            var user = new User
            {
                Email = email,
                Role = "Teacher",
                PasswordForHashing = password
            };

            var createdTrainings = new Dictionary<string, List<string>>();
            if (groupsAndNumTrainings != null)
            {
                if (trainingPlanName == null)
                    throw new ArgumentException($"{nameof(trainingPlanName)} cannot be null");

                foreach (var (group, numTrainings) in groupsAndNumTrainings)
                {
                    if (!user.Trainings.ContainsKey(group))
                        user.Trainings.Add(group, new());

                    createdTrainings.Add(group, new());

                    for (int i = 0; i < numTrainings; i++)
                    {
                        var training = actuallyCreate
                            ? await trainingRepository.Add(trainingPlanRepository, trainingUsernameService, trainingPlanName, settings)
                            : new Training { Id = 1, Username = "FakeTraining" };
                        user.Trainings[group].Add(training.Id);
                        createdTrainings[group].Add(training.Username);
                    }
                }
            }

            if (actuallyCreate)
                await userRepository.Upsert(user);

            return new CreateUserResult { User = user, WasCreated = true, Password = password, CreatedTrainings = createdTrainings };
        }

        public static List<MailAddress> ReadEmails(string emailsFile)
        {
            var rows = File.ReadAllLines(emailsFile).SelectMany(o => o.Split(',', ';'))
                .Select(o => o.Trim())
                .Where(o => o.Length > 4)
                .Select(o => new { Address = MailAddress.TryCreate(o, out var address) ? address : null, String = o });
            var invalids = rows.Where(o => o.Address == null).ToList();
            if (invalids.Any())
            {
                throw new Exception("Invalid");
            }
            return rows.Select(o => o.Address).OfType<MailAddress>().ToList();
        }

        public static List<CreateUserResult> CreateDummyUserList(IEnumerable<string> emails)
        {
            return emails.Select(email =>
                new CreateUserResult
                {
                    User = new User { Email = email },
                    WasCreated = true,
                    Password = "bla blabla",
                    CreatedTrainings = new Dictionary<string, List<string>> { { "Test", new() { "ajaj fofo", "nubbe sddfd" } } }
                }).ToList();
        }

        public async Task CreateAndEmail(IConfiguration config, bool actuallyCreate = false)
        {
            var rootPath = @"C:\Users\uzk446\Downloads\";
            var useJsonFile = $"{rootPath}createdUsers.json";
            List<CreateUserResult>? createdUsersInfo;
            if (File.Exists(useJsonFile))
                createdUsersInfo = JsonConvert.DeserializeObject<List<CreateUserResult>>(File.ReadAllText(useJsonFile));
            else
            {
                //var emails = ReadEmails($"{rootPath}TeacherEmails.txt").Select(o => o.ToString()).ToList();
                var emails = new List<string>(); 
                createdUsersInfo = (await CreateUsers(emails, new Dictionary<string, int> { { "Test", 2 } }, "2018 VT template Default", actuallyCreate)).ToList();
                File.WriteAllText(useJsonFile.Replace(".json", $"-{DateTime.Now:dd_HH_mm}.json"), JsonConvert.SerializeObject(createdUsersInfo));
                File.WriteAllText(useJsonFile, JsonConvert.SerializeObject(createdUsersInfo));
            }
            if (createdUsersInfo == null)
                return;

            var batchSize = 100;
            var batchNum = 0;
            createdUsersInfo = createdUsersInfo.Chunk(batchSize).Skip(batchNum).First().ToList();
            try
            {
                await BatchMail.SendInvitations(config, createdUsersInfo, actuallySend: actuallyCreate);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            File.WriteAllText($"{rootPath}Sent-{DateTime.Now:dd_HH_mm}.json", JsonConvert.SerializeObject(createdUsersInfo.Select(o => o.User.Email)));
        }
    }
}
