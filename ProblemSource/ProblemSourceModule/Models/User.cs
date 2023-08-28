using ProblemSource.Services;
using ProblemSourceModule.Models.Aggregates;
using ProblemSourceModule.Services.Storage;

namespace ProblemSourceModule.Models
{
    public class User
    {
        public string Email { get; set; } = string.Empty;
        public string HashedPassword { get; set; } = string.Empty;
        public string Role { get; set; } = "";

        public string PasswordForHashing { set { HashedPassword = HashPassword(NormalizeEmail(Email), value); } }

        public bool VerifyPassword(string password)
        {
            var tmp = new User { Email = Email, PasswordForHashing = password };
            return tmp.HashedPassword == HashedPassword;
        }

        public UserTrainingsCollection Trainings { get; set; } = new();

        public static string NormalizeEmail(string email) => email.ToLower().Trim();

        private static string HashPassword(string saltBase, string password)
        {
            //var salt = System.Security.Cryptography.RandomNumberGenerator.GetBytes(128 / 8); // divide by 8 to convert bits to bytes
            var salt = System.Text.Encoding.UTF8.GetBytes(saltBase.PadLeft(128 / 8, '0'));

            // derive a 256-bit subkey (use HMACSHA256 with 100,000 iterations)
            return Convert.ToBase64String(
                Microsoft.AspNetCore.Cryptography.KeyDerivation.KeyDerivation.Pbkdf2(
                    password: password!,
                    salt: salt,
                    prf: Microsoft.AspNetCore.Cryptography.KeyDerivation.KeyDerivationPrf.HMACSHA256,
                    iterationCount: 100000,
                    numBytesRequested: 256 / 8));
        }
    }

    public class UserTrainingsCollection : Dictionary<string, List<int>>
    {
        public UserTrainingsCollection()
        {
        }

        public UserTrainingsCollection(string groupName, IEnumerable<int> ids)
        {
            Add(groupName, ids.ToList());
        }

        public UserTrainingsCollection(Dictionary<string, IEnumerable<int>> groups)
        {
            foreach (var kv in groups)
                Add(kv.Key, kv.Value.ToList());
        }
        public UserTrainingsCollection(Dictionary<string, List<int>> groups)
        {
            foreach (var kv in groups)
                Add(kv.Key, kv.Value);
        }

        public List<int> GetAllIds()
        {
            return Values.SelectMany(o => o).ToList();
        }

        public async Task<Dictionary<string, List<(int Id, Training Training, TrainingSummary? Summary)>>> GetTrainingsInfo(ITrainingRepository trainingRepo, IStatisticsProvider statisticsProvider)
        {
            var summaries = await statisticsProvider.GetTrainingSummaries(GetAllIds());
            var trainings = await trainingRepo.GetByIds(GetAllIds());

            var result = new Dictionary<string, List<(int, Training, TrainingSummary?)>>();

            foreach (var kv in this)
                result.Add(kv.Key, kv.Value.Select(id => (id, trainings.Single(o => o.Id == id), summaries.SingleOrDefault(o => o?.Id == id))).ToList());

            return result;
        }

        public async Task<Dictionary<string, List<int>>> RemoveUnusedFromGroups(int numTrainings, string exceptGroup, ITrainingRepository trainingRepo, IStatisticsProvider statisticsProvider)
        {
            var info = await GetTrainingsInfo(trainingRepo, statisticsProvider);

            var summaries = info.SelectMany(o => o.Value).Select(o => o.Item3).OfType<TrainingSummary>();
            var startedTrainingIds = summaries.Where(o => o.TrainedDays >= 1).Select(o => o.Id).ToList();

            var trainings = info.SelectMany(o => o.Value).Select(o => o.Item2).OfType<Training>();
            var justCreatedTrainingIds = trainings.Where(o => o.Created >= DateTimeOffset.UtcNow.AddDays(-1)).Select(o => o.Id).ToList(); ;

            var numRemainingToTransfer = numTrainings;

            var updated = new Dictionary<string, List<int>>();
            var source = new Dictionary<string, List<int>>();

            foreach (var kv in this.Where(o => o.Key != exceptGroup))
            {
                var available = kv.Value.Except(startedTrainingIds).Except(justCreatedTrainingIds);
                var forTransfer = available.Take(numRemainingToTransfer);

                if (forTransfer.Any())
                {
                    source[kv.Key] = forTransfer.ToList();
                    updated[kv.Key] = kv.Value.Except(forTransfer).ToList();
                }

                numRemainingToTransfer -= forTransfer.Count();
                if (numRemainingToTransfer == 0)
                    break;
            }

            return source;
            // Note: caller updates user repo
        }
    }
}
