using Shouldly;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Organization.Tests
{
    public class TagQueryRenderTests
    {
        [Theory]
        //[InlineData(@"-1 && A && ""1"" && (2 || 3) && !4")]
        //[InlineData(@"!1", "NOT IN ( act_id IN (SELECT act_id FROM m2m WHERE grp_id = 1) )")]
        // "act_id NOT IN (SELECT act_id FROM m2m WHERE actId IN (SELECT act_id FROM m2m WHERE grp_id = 1) )"
        [InlineData(@"1 || 2",
            "SELECT DISTINCT account_id FROM accounts_groups WHERE account_id IN (SELECT account_id FROM accounts_groups WHERE (group_id = 1)) OR account_id IN (SELECT account_id FROM accounts_groups WHERE (group_id = 2))")]
        [InlineData(@"'Teacher 77' && '_country SE' && '_muni Sollentuna' && '_school Rösjöskolan'",
            "SELECT DISTINCT account_id FROM accounts_groups WHERE account_id IN (SELECT account_id FROM accounts_groups WHERE (group_id IN (SELECT id FROM groups WHERE name = 'Teacher 77'))) AND account_id IN (SELECT account_id FROM accounts_groups WHERE (group_id IN (SELECT id FROM groups WHERE name = '_country SE'))) AND account_id IN (SELECT account_id FROM accounts_groups WHERE (group_id IN (SELECT id FROM groups WHERE name = '_muni Sollentuna'))) AND account_id IN (SELECT account_id FROM accounts_groups WHERE (group_id IN (SELECT id FROM groups WHERE name = '_school Rösjöskolan')))")]
        [InlineData(@"414 AND (438 OR 439)",
            "SELECT DISTINCT account_id FROM accounts_groups WHERE account_id IN (SELECT account_id FROM accounts_groups WHERE (group_id = 414)) AND ( account_id IN (SELECT account_id FROM accounts_groups WHERE (group_id = 438)) OR account_id IN (SELECT account_id FROM accounts_groups WHERE (group_id = 439)) )")]
        //[InlineData(@"!(1)",
        //    "TODO")]
        //[InlineData(@"!1",
        //    "TODO")]
        //[InlineData(@"!(1 || 2)", "TODO")]
        public void BooleanExpressionToSql(string input, string expected)
        {
            var str = RenderSqlExpression(input, true);
            str.ShouldBe(expected);
        }

        private string? RenderSqlExpression(string input, bool convertFromOldFormat = false)
        {
            if (convertFromOldFormat)
            {
                input = input
                    .Replace('\'', '"')
                    .Replace(" AND ", " && ")
                    .Replace(" OR ", " || ")
                    .Replace(" NOT", " !");
            }
            var m2mTable = new SqlGroupExpressionRenderer.M2MTableConfig();
            var groupTable = new SqlGroupExpressionRenderer.GroupTableConfig();
            var renderer = new SqlGroupExpressionRenderer(m2mTable, groupTable);

            return BooleanExpressionTree.ParseAndRender(input, renderer);
        }

        [Theory]
        [InlineData("414 && (438 || 439)", "aa IN (414) AND ( aa IN (438) OR aa IN (439) )")]
        public void BooleanExpressionSimpleRender(string input, string expected)
        {
            var str = BooleanExpressionTree.ParseAndRender(input, new SimpleExpressionRenderer());
            str.ShouldBe(expected);
        }

        public class SimpleExpressionRenderer : BooleanExpressionTree.ExpressionRenderer
        {
            public override string Render(ExpressionStatementSyntax exp) => string.Empty;

            public override string Render(LiteralExpressionSyntax exp)
            {
                return $"aa IN ({exp})";
                //return exp.Kind() == SyntaxKind.StringLiteralExpression
                //    ? $"aa IN ({exp})"
                //    : $"{exp}";
            }

            public override string Render(IdentifierNameSyntax exp) => exp.ToString();

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
                {
                    var hasParenthesizedChild = exp.ChildNodes().First() is ParenthesizedExpressionSyntax;
                    return "NOT IN " + (hasParenthesizedChild ? "" : "(");
                }
                return string.Empty;
            }

            public override string PostRender(PrefixUnaryExpressionSyntax exp)
            {
                var hasParenthesizedChild = exp.ChildNodes().First() is ParenthesizedExpressionSyntax;
                return hasParenthesizedChild ? "" : ")";
            }
        }
    }
}
