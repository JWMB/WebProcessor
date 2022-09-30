namespace ProblemSource
{
    public class MnemoJapanese
    {
        private readonly static Random _rnd = new Random();
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

        public static string FromIntWithRandom(int value, int numRandomDigits)
        {
            var lower = (int)Math.Pow(10, numRandomDigits - 1);
            var upper = (int)Math.Pow(10, numRandomDigits);
            return FromInt(int.Parse("" + (_rnd.Next(upper - lower) + lower) + "" + value));
        }

        public static int? ToIntWithRandom(string value)
        {
            var decoded = ToInt(value);
            if (decoded == null)
                return null;
            return int.Parse(decoded.Value.ToString().Substring(2));
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

        public static void Test()
        {
            var dbg = "";
            var rnd = new Random();
            for (int i = 0; i < 100; i++)
            {
                var val = 280000 + i;
                //depending on last digits, scramble number for more uniqueness between sequential items
                //var c = int.Parse("" + val.ToString()[0]);

                //var addFirst = rnd.Next(90) + 10; //Always two extra digits first
                //var useVal = int.Parse(addFirst.ToString() + val);
                var str = FromIntWithRandom(val, 2);
                var decoded = ToIntWithRandom(str);
                dbg += str + " " + val; //+ " " + useVal;
                if (int.Parse(decoded.ToString().Substring(2)) != val)
                    dbg += " err: " + decoded;
                dbg += "\r\n";
            }
        }
    }
}
