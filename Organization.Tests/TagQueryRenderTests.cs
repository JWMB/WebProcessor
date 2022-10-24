using Shouldly;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Organization.Tests
{
    public class TagQueryRenderTests
    {
        [Fact]
        public void ASTTest()
        {
            var input = @"-1 && A && ""1"" && (2 || 3) && !4";
            input = "!1";
            input = "!(1 || 2)";
            var str = BooleanExpressionTree.ParseExperiment(input, new SqlGroupExpressionRenderer());
        }

        public class SqlGroupExpressionRenderer : BooleanExpressionTree.ExpressionRenderer
        {
            private readonly string baseQuery = "account_id {0} IN (SELECT account_id FROM accounts_groups WHERE";
            private string GroupNamePredicate(string groupName) => $"{baseQuery} group_id IN (SELECT id FROM groups WHERE name = '{groupName}')) )";

            public override string Render(LiteralExpressionSyntax exp)
            {
                if (exp.Kind() == Microsoft.CodeAnalysis.CSharp.SyntaxKind.StringLiteralExpression)
                    return GroupNamePredicate(exp.ToString().Trim('\"'));
                return $"{baseQuery} group_id = {exp})";
            }

            public override string Render(IdentifierNameSyntax exp) => GroupNamePredicate($"{exp}");

            public override string Render(BinaryExpressionSyntax exp)
            {
                switch (exp.OperatorToken.ToString())
                {
                    case "&&":
                        return "AND";
                    case "||":
                        return "OR";
                    default:
                        throw new Exception($"Unhandled operator {exp.OperatorToken}");
                }
            }

            public override string Render(PrefixUnaryExpressionSyntax exp)
            {
                if (exp.OperatorToken.ToString() == "!")
                    return "NOT"; // TODO: hm, should be AND NOT if not at beginning of current subclause
                return String.Empty;
                //TODO: throw new Exception($"Unhandled operator {exp.OperatorToken}");
            }
        }
    }
}
