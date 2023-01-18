using ProblemSource.Services.Storage;
using ProblemSourceModule.Models;
using ProblemSourceModule.Services;
using ProblemSourceModule.Services.Storage;

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

        public async Task<IEnumerable<CreateUserResult>> CreateUsers(IEnumerable<string> emails, Dictionary<string, int>? groupsAndNumTrainings = null)
        {
            var result = new List<CreateUserResult>();
            foreach (var email in emails)
            {
                var user = await CreateUser(email, groupsAndNumTrainings);
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
        }

        public async Task<CreateUserResult> CreateUser(string email, Dictionary<string, int>? groupsAndNumTrainings = null)
        {
            var existing = await userRepository.Get(email);
            if (existing != null)
                return new CreateUserResult { User = existing, WasCreated = false };

            var password = string.Join("", Enumerable.Range(0, 6).Select(o => (char)rnd.Next(48, 90)));
            var user = new User
            {
                Email = email,
                Role = "Teacher",
                HashedPassword = User.HashPassword(email, password)
            };

            var createdTrainings = new Dictionary<string, List<string>>();
            if (groupsAndNumTrainings != null)
            {
                foreach (var (group, numTrainings) in groupsAndNumTrainings)
                {
                    if (!user.Trainings.ContainsKey(group))
                        user.Trainings.Add(group, new());

                    createdTrainings.Add(group, new());

                    for (int i = 0; i < numTrainings; i++)
                    {
                        var training = await trainingRepository.Add(trainingPlanRepository, trainingUsernameService, "2018 VT template Default", null);
                        user.Trainings[group].Add(training.Id);
                        createdTrainings[group].Add(training.Username);
                    }
                }
            }
            await userRepository.Upsert(user);

            return new CreateUserResult { User = user, WasCreated = true, Password = password, CreatedTrainings = createdTrainings };
        }
    }
}
