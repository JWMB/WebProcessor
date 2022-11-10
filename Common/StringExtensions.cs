using System.Globalization;

namespace Common
{
    public static class StringExtensions
    {
        public static IEnumerable<string> SplitByLength(this string value, int maxLength)
        {
            for (int index = 0; index < value.Length; index += maxLength)
            {
                yield return value.Substring(index, Math.Min(maxLength, value.Length - index));
            }
        }
        //// https://codereview.stackexchange.com/questions/111919/split-a-string-into-chunks-of-the-same-length
        //// But skips a character in each chunk?!
        //public static IEnumerable<string> Split(this string value, int desiredLength)
        //{
        //    var characters = StringInfo.GetTextElementEnumerator(value);
        //    do
        //    {
        //        yield return String.Concat(characters.AsEnumerable<string>().Take(desiredLength));
        //    } while (characters.MoveNext());
        //}

        //public static IEnumerable<T> AsEnumerable<T>(this System.Collections.IEnumerator enumerator)
        //{
        //    while (enumerator.MoveNext())
        //        yield return (T)enumerator.Current;
        //}
    }
}
