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
        [InlineData(@"1 || 2", "act_id IN (SELECT act_id FROM m2m WHERE grp_id = 1) OR act_id IN (SELECT act_id FROM m2m WHERE grp_id = 2)")]
        //[InlineData(@"!(1)")]
        //[InlineData(@"!(1 || 2)")]
        public void ASTTest(string input, string expected)
        {
            var renderer = new SqlGroupExpressionRenderer(
                new SqlGroupExpressionRenderer.M2MTableConfig { TableName = "m2m", GroupIdColumnName = "grp_id", OtherIdColumnName = "act_id" },
                new SqlGroupExpressionRenderer.GroupTableConfig { TableName = "grp", IdColumnName = "id", NameColumnName = "name" }
                );
            var str = BooleanExpressionTree.ParseExperiment(input, renderer);
            // SELECT act_id FROM grp WHERE {str}
            str.ShouldBe(expected);
        }

        public class SqlGroupExpressionRenderer : BooleanExpressionTree.ExpressionRenderer
        {
            public class GroupTableConfig
            {
                public string TableName { get; set; }
                public string IdColumnName { get; set; }
                public string NameColumnName { get; set; }
            }
            public class M2MTableConfig
            {
                public string TableName { get; set; }
                public string GroupIdColumnName { get; set; }
                public string OtherIdColumnName { get; set; }
            }

            //private readonly string m2mGroupIdName = "group_id";
            //private readonly string m2mOtherIdName = "account_id";
            //private readonly string m2mTableName = "accounts_groups";

            //private readonly string groupTableName = "groups";
            //private readonly string groupNameColumnName = "name";
            //private readonly string groupIdColumnName = "id";

            private readonly string baseQuery;
            private readonly M2MTableConfig m2mTable;
            private readonly GroupTableConfig groupTable;

            private string GroupNamePredicate(string groupName) => $"{baseQuery} {m2mTable.GroupIdColumnName} IN (SELECT {groupTable.IdColumnName} FROM {groupTable.TableName} WHERE {groupTable.NameColumnName} = '{groupName}')) )";

            public SqlGroupExpressionRenderer(M2MTableConfig m2mTable, GroupTableConfig groupTable)
            {
                this.m2mTable = m2mTable;
                this.groupTable = groupTable;
                baseQuery = $"{m2mTable.OtherIdColumnName} IN (SELECT {m2mTable.OtherIdColumnName} FROM {m2mTable.TableName} WHERE"; //was: account_id {0} IN ...
            }

            public override string Render(LiteralExpressionSyntax exp)
            {
                if (exp.Kind() == Microsoft.CodeAnalysis.CSharp.SyntaxKind.StringLiteralExpression)
                    return GroupNamePredicate(exp.ToString().Trim('\"'));
                return $"{baseQuery} {m2mTable.GroupIdColumnName} = {exp})";
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

            //public override string Render(PrefixUnaryExpressionSyntax exp)
            //{
            //    // Hm, would the easiest be to nest the following expression in parenthesis, like "id NOT IN (<expression>)"
            //    // But here we're not able to modify the structure... Rethink the logic?
            //    if (exp.OperatorToken.ToString() == "!")
            //        return "NOT"; // TODO: should be AND NOT if it's not at the beginning of current subclause?
            //    return String.Empty;
            //    //TODO: throw new Exception($"Unhandled operator {exp.OperatorToken}");
            //}
            public override string Render(PrefixUnaryExpressionSyntax exp)
            {
                if (exp.OperatorToken.ToString() == "!")
                {
                    var hasParenthesizedChild = exp.ChildNodes().First() is ParenthesizedExpressionSyntax;
                    return "NOT IN " + (hasParenthesizedChild ? "" : "(");
                }
                return String.Empty;
            }

            public override string PostRender(PrefixUnaryExpressionSyntax exp)
            {
                var hasParenthesizedChild = exp.ChildNodes().First() is ParenthesizedExpressionSyntax;
                return hasParenthesizedChild ? "" : ")";
            }
        }
    }
}
