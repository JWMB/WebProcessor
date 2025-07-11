﻿namespace ProblemSource
{
    public class MnemoJapanese
    {
        private readonly static Random rnd = new Random();
        private readonly int numRandomDigits;

        private static List<string>? symbols = null;
        public static List<string> Symbols
        {
            get
            {
                if (symbols == null)
                {
                    var vow = "a e i o u".Split(' ');
                    symbols = "b d g h j k m n p r s t z".Split(' ')
                        .SelectMany(c => vow.Select(v => c + v))
                        .Concat("wa wo ya yo yu".Split(' ')).ToList();
                }
                return symbols;
            }
        }

        public MnemoJapanese(int numRandomDigits = 0)
        {
            this.numRandomDigits = numRandomDigits;
        }

        public string FromIntWithRandom(int value)
        {
            var lower = (int)Math.Pow(10, numRandomDigits - 1);
            var upper = (int)Math.Pow(10, numRandomDigits);
            return FromInt(int.Parse("" + (rnd.Next(upper - lower) + lower) + "" + value));
        }

        public int? ToIntWithRandom(string value)
        {
            var decoded = ToInt(value);
            if (decoded == null)
                return null;
            if (decoded.Value.ToString().Length <= numRandomDigits)
                return null;
            return int.TryParse(decoded.Value.ToString().Substring(numRandomDigits), out var result) ? result : null;
        }

        public static string FromInt(int value)
        {
            var mod = value % Symbols.Count;
            var rest = value / Symbols.Count;
            return Symbols[mod] + (rest > 0 ? FromInt(rest) : "");
        }

        public static int? ToInt(string value)
        {
            var index = Symbols.FindIndex(_ => value.IndexOf(_) == 0);
            if (index < 0)
                return null;
            var syll = Symbols[index];
            return index + (value.Length > syll.Length ? Symbols.Count * ToInt(value.Substring(syll.Length)) : 0);
        }
    }

    public class BaseNNumber
    {
        private List<int> parts = new();
        public BaseNNumber(int baseX)
        {
            BaseX = baseX;
        }

        public int BaseX { get; }
        public IReadOnlyList<int> Parts => parts;

        public static BaseNNumber CreateFromNumber(int baseX, int value)
        {
            var result = new BaseNNumber(baseX);
            var tmp = new List<int>();
            while (value > 0) {
                var val = value % baseX;
                tmp.Add(val);
                value = (int)Math.Floor((decimal)value / baseX);
            }
            tmp.Reverse();
            result.parts = tmp;
            return result;
        }

        public int ToNumber()
        {
            var fact = 1;
            var result = 0;
            for (var i = parts.Count - 1; i >= 0; i--)
            {
                result += parts[i] * fact;
                fact *= BaseX;
            }
            return result;
        }

        public BaseNNumber ToOtherBase(int baseX) => CreateFromNumber(baseX, ToNumber());
    }

    public class UsernameHashing
    {
        // TODO: not well composed, uses static MnemoJapanese methods
        private readonly int numHashCharacters;
        private readonly MnemoJapanese mnemoJapanese;

		public UsernameHashing(MnemoJapanese mnemoJapanese, int numHashCharacters = 2)
        {
            this.mnemoJapanese = mnemoJapanese;
            this.numHashCharacters = numHashCharacters;
        }

        public string Hash(string username)
        {
            var id = mnemoJapanese.ToIntWithRandom(username);
            if (id == null) // Was this for backward compatibility..?
                return username;

            var calculatedHash = CalculateHash(id.Value);

            var splitIndex = 2;
            return $"{MnemoJapanese.FromInt(calculatedHash)}{username.Substring(0, splitIndex)} {username.Substring(splitIndex)}";
        }

        public static bool IsHashed(string username) => username.Contains(" ");

        private int CalculateHash(int value)
        {
            var bn = BaseNNumber.CreateFromNumber(MnemoJapanese.Symbols.Count, value);
            var sum = bn.Parts.Sum();
            return sum % bn.BaseX;
        }

        public string? Dehash(string hashed_username)
        {
            if (!IsHashed(hashed_username))
                return null;

            var original = hashed_username;
            hashed_username = hashed_username.Replace(" ", "");

            var hashString = hashed_username.Substring(0, numHashCharacters);
            var hashValue = MnemoJapanese.ToInt(hashString);
            if (hashValue == null)
                return null;

            if (hashed_username.Length <= numHashCharacters)
                throw new ArgumentException($"hashed_username too short after truncation: '{hashed_username}' (org: '{original}')");

            var username = hashed_username.Substring(numHashCharacters);
            var id = mnemoJapanese.ToIntWithRandom(username);
            if (id == null)
                return null;

            var calculatedHash = CalculateHash(id.Value);

            if (hashValue != calculatedHash)
                return null;

            return username;
        }

		public bool TryGetTrainingIdFromUsername(string uuid, bool validateOnly, out int trainingId)
		{
			// TODO: use regular asp.net model validation
			trainingId = -1;

			// TODO: use regular model validation
			if (string.IsNullOrEmpty(uuid))
				return false;

			// Handle common user input mistakes:
			uuid = uuid.Trim().Replace("  ", " ");

			// TODO: client has already dehashed (but should not, let server handle ui)
			var dehashedUuid = uuid.Contains(" ") ? Dehash(uuid) : uuid;

			if (dehashedUuid == null)
			{
				if (validateOnly)
				{
					if (!uuid.Contains(" ") && uuid.Length > 4) // allow for forgotten space
						dehashedUuid = Dehash(uuid.Insert(4, " "));

					if (dehashedUuid == null)
						return false;
				}
				else
					return false;
			}

			var id = mnemoJapanese.ToIntWithRandom(dehashedUuid);
			if (id == null)
				return false;

			trainingId = id.Value;

			return true;
		}
	}
}
