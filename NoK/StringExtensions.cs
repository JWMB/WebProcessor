using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace NoK.Tests
{
    public static class StringExtensions
    {
        public static string ReplaceRx(this string value, [StringSyntax(StringSyntaxAttribute.Regex)] string regexPattern, string replacement) => new Regex(regexPattern).Replace(value, replacement);

        public static string ReplaceRx(this string value, [StringSyntax(StringSyntaxAttribute.Regex)] string regexPattern, MatchEvaluator evaluator) => new Regex(regexPattern).Replace(value, evaluator);
    }
}