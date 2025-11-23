using ProblemSource.Models;
using ProblemSource.Services.Storage;
using ProblemSourceModule.Models;
using ProblemSourceModule.Services.Storage;

namespace ProblemSourceModule.Services
{
    public class CreateUserWithTrainings
    {
        private readonly IUserRepository userRepository;
        private readonly ITrainingRepository trainingRepository;
        private readonly ITrainingPlanRepository trainingPlanRepository;
        private readonly ITrainingUsernameService trainingUsernameService;

        private readonly Random rnd = new Random();

		public CreateUserWithTrainings(IUserRepository userRepository, ITrainingRepository trainingRepository, ITrainingPlanRepository trainingPlanRepository,
			ITrainingUsernameService trainingUsernameService)
        {
            this.userRepository = userRepository;
            this.trainingRepository = trainingRepository;
            this.trainingPlanRepository = trainingPlanRepository;
            this.trainingUsernameService = trainingUsernameService;
        }

		public string CreatePassword()
		{
			var pwdChars = Enumerable.Range(48, 10)
				.Concat(Enumerable.Range(65, 25))
				.Concat(Enumerable.Range(97, 25))
				.Select(o => (char)o)
				.ToList();
			return string.Join("", Enumerable.Range(0, 6).Select(o => pwdChars[rnd.Next(pwdChars.Count)]));
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

		public async Task<CreateUserResult> CreateUser(string email, Dictionary<string, int>? groupsAndNumTrainings = null,
			string? trainingPlanName = null, TrainingSettings? settings = null, bool actuallyCreate = false)
		{
			var existing = await userRepository.Get(email);
			if (existing != null)
				return new CreateUserResult { User = existing, WasCreated = false };

			settings ??= new TrainingSettings { timeLimits = new List<decimal> { 33 } };

			var password = CreatePassword();
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

				var tp = await trainingPlanRepository.Get(trainingPlanName);
				if (tp == null)
					throw new Exception($"Training plan not found: {trainingPlanName}");

				foreach (var (group, numTrainings) in groupsAndNumTrainings)
				{
					if (!user.Trainings.ContainsKey(group))
						user.Trainings.Add(group, new());

					createdTrainings.Add(group, new());

					for (int i = 0; i < numTrainings; i++)
					{
						var training = new Training
						{
							TrainingPlanName = trainingPlanName,
							Settings = settings ?? TrainingSettings.Default,
							Created = DateTimeOffset.UtcNow,
							Username = actuallyCreate ? string.Empty : "FakeTraining"
						};

						if (actuallyCreate)
						{
							await trainingRepository.Add(trainingUsernameService, training);
						}

						user.Trainings[group].Add(training.Id);
						createdTrainings[group].Add(training.Username);
					}
				}
			}

			if (actuallyCreate)
				await userRepository.Upsert(user);

			return new CreateUserResult { User = user, WasCreated = true, Password = password, CreatedTrainings = createdTrainings };
		}
	}
}
