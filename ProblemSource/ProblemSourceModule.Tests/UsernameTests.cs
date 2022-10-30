using Shouldly;

namespace ProblemSource.Tests
{
    public class UsernameTests
    {
        [Fact]
        public void MnemoJapanese_GenerateList()
        {
            var dbg = new Dictionary<string, int>();
            var mnemoJapanese = new MnemoJapanese(2);
            //var numExtra = 2;
            for (int i = 0; i < 100; i++)
            {
                var val = 280000 + i;
                //TODO: ? depending on last digits, scramble number for more uniqueness between sequential items

                var str = mnemoJapanese.FromIntWithRandom(val);
                var decoded = mnemoJapanese.ToIntWithRandom(str);
                if (decoded == null)
                    throw new Exception($"'{str}' couldn't be decoded (from {val})");
                //if (int.Parse(decoded.Value.ToString().Substring(numExtra)) != val)
                if (decoded.Value != val)
                    throw new Exception($"'{str}' was decoded to {decoded} instead of {val}");

                dbg.Add(str, val);
            }
        }

        [Fact]
        public void Hash_Dehash()
        {
            var mnemoJapanese = new MnemoJapanese(2);
            var hashedUsername = new HashedUsername(mnemoJapanese, 2);

            var id = 1000;
            var username = mnemoJapanese.FromIntWithRandom(id);
            var hashed = hashedUsername.Hash(username);

            var dehashed = hashedUsername.Dehash(hashed);
            if (dehashed == null)
                throw new Exception($"Could not dehash {hashed} (value {id})");

            var decodedId = mnemoJapanese.ToIntWithRandom(dehashed);

            decodedId.ShouldBe(id);
        }

        [Fact]
        public void Dehash_IncorrectChecksum_ReturnsNull()
        {
            var mnemoJapanese = new MnemoJapanese(2);
            var hashedUsername = new HashedUsername(mnemoJapanese, 2);

            var id = 1000;
            var username = mnemoJapanese.FromIntWithRandom(id);
            var hashed = hashedUsername.Hash(username);
            hashed = $"{(hashed[0] == 'b' ? 'a' : 'b')}{hashed.Substring(1)}";
            var dehashed = hashedUsername.Dehash(hashed);

            dehashed.ShouldBeNull();
        }
    }
}
