using Shouldly;

namespace ProblemSource.Tests
{
    public class UsernameTests
    {
        private readonly MnemoJapanese mnemoJapanese = new MnemoJapanese(2);
        private readonly UsernameHashing hashedUsername;
        public UsernameTests()
        {
            hashedUsername = new UsernameHashing(mnemoJapanese, 2);
        }

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
            var id = 5; // 1:beje gi  2:bibi gi  3:bojo du  5:daha du  10:gaba jobe
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
            var id = 1000;
            var username = mnemoJapanese.FromIntWithRandom(id);
            var hashed = hashedUsername.Hash(username);
            hashed = $"{(hashed[0] == 'b' ? 'a' : 'b')}{hashed.Substring(1)}";
            var dehashed = hashedUsername.Dehash(hashed);

            dehashed.ShouldBeNull();
        }
    }
}
