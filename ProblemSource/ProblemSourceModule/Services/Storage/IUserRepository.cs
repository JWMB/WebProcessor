namespace ProblemSourceModule.Services.Storage
{
    public interface IUserRepository : IRepository<User, string>
    {
    }

    public class User
    {
        public string Email { get; set; } = string.Empty;
        public string HashedPassword { get; set; } = string.Empty;
        public string Role { get; set; } = "Admin";

        public List<int> Trainings { get; set; } = new();

        public static string HashPassword(string saltBase, string password)
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
}
