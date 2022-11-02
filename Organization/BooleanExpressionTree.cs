using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Organization
{
    public class BooleanExpressionTree
    {
        public class ExpressionRenderer
        {
            public virtual string Render(ExpressionStatementSyntax exp) => string.Empty;
            public virtual string PostRender(ExpressionStatementSyntax exp) => string.Empty;

            public virtual string Render(LiteralExpressionSyntax exp) => exp.ToString();
            public virtual string Render(IdentifierNameSyntax exp) => exp.ToString();
            public virtual string Render(BinaryExpressionSyntax exp) => exp.OperatorToken.ToString();

            public virtual string Render(PrefixUnaryExpressionSyntax exp) => exp.OperatorToken.ToString();
            public virtual string PostRender(PrefixUnaryExpressionSyntax exp) => string.Empty;

            public virtual string Finalize(IEnumerable<string> items) => string.Join(" ", items.Where(o => o.Any()));
        }

        private static CompilationUnitSyntax? ParseGetCompilationRoot(string input)
        {
            var tree = CSharpSyntaxTree.ParseText(input);
            if (tree == null)
                return null;
            return tree.GetCompilationUnitRoot();
        }

        public static string? ParseAndRender(string input, ExpressionRenderer? renderer = null)
        {
            var root = ParseGetCompilationRoot(input);
            if (root == null)
                return null;

            var expressionRoot = root.DescendantNodes().FirstOrDefault(o => o is ExpressionStatementSyntax);
            if (expressionRoot == null)
                return null;

            if (renderer == null)
                renderer = new ExpressionRenderer();

            var list = new List<string>();
            Recurse(expressionRoot, list);

            void Recurse(SyntaxNode parent, List<string> output)
            {
                var indent = string.Join("", Enumerable.Range(0, parent.Ancestors().Count()).Select(o => " "));

                if (parent is LiteralExpressionSyntax literal)
                {
                    output.Add(renderer.Render(literal));
                }
                else if (parent is IdentifierNameSyntax identifier)
                {
                    output.Add(renderer.Render(identifier));
                }
                else if (parent is ParenthesizedExpressionSyntax paren)
                {
                    output.Add(paren.OpenParenToken.ToString());
                    DoChildren(paren);
                    output.Add(paren.CloseParenToken.ToString());
                }
                else if (parent is BinaryExpressionSyntax binary)
                {
                    DoParentAndChildren(binary.Left);
                    output.Add(renderer.Render(binary));
                    DoParentAndChildren(binary.Right);
                }
                else if (parent is PrefixUnaryExpressionSyntax unary)
                {
                    // Maybe only allow ! and -
                    output.Add(renderer.Render(unary));
                    DoChildren(parent);
                    output.Add(renderer.PostRender(unary));
                }
                else if (parent is ExpressionStatementSyntax exp)
                {
                    output.Add(renderer.Render(exp));
                    DoChildren(parent);
                    output.Add(renderer.PostRender(exp));
                }
                else
                    throw new Exception($"{parent.GetType().Name} {parent.Kind()} {parent.GetText()}");

                void DoParentAndChildren(SyntaxNode p) => Recurse(p, output);
                void DoChildren(SyntaxNode p) => p.ChildNodes().ToList().ForEach(o => Recurse(o, output));
            }
            return renderer.Finalize(list);
        }

        public static string TreeToString(SyntaxNode parent)
        {
            return string.Join("\n", Traverse(parent));
            IEnumerable<string> Traverse(SyntaxNode parent)
            {
                var indent = string.Join("", Enumerable.Range(0, parent.Ancestors().Count()).Select(o => " "));
                yield return $"{indent}{parent.GetType().Name} {parent.Kind()} {parent.GetText()}";
                foreach (var item in parent.ChildNodes())
                    foreach (var x in Traverse(item))
                        yield return x;
            }
        }
    }

    public class SqlGroupExpressionRenderer : BooleanExpressionTree.ExpressionRenderer
    {
        public class GroupTableConfig
        {
            public string TableName { get; set; } = "groups";
            public string IdColumnName { get; set; } = "id";
            public string NameColumnName { get; set; } = "name";
        }
        public class M2MTableConfig
        {
            public string TableName { get; set; } = "accounts_groups";
            public string GroupIdColumnName { get; set; } = "group_id";
            public string OtherIdColumnName { get; set; } = "account_id";
        }

        private readonly string baseQuery;
        private readonly M2MTableConfig m2mTable;
        private readonly GroupTableConfig groupTable;

        private string GroupNamePredicate(string groupName) => string.Format(baseQuery, $"{m2mTable.GroupIdColumnName} IN (SELECT {groupTable.IdColumnName} FROM {groupTable.TableName} WHERE {groupTable.NameColumnName} = '{groupName}')");
        private string GroupIdPredicate(string groupId) => string.Format(baseQuery, $"{m2mTable.GroupIdColumnName} = {groupId}");

        public SqlGroupExpressionRenderer(M2MTableConfig m2mTable, GroupTableConfig groupTable)
        {
            this.m2mTable = m2mTable;
            this.groupTable = groupTable;
            baseQuery = $"{m2mTable.OtherIdColumnName} IN (SELECT {m2mTable.OtherIdColumnName} FROM {m2mTable.TableName} WHERE ({{0}}))"; //was: account_id {0} IN ...

        }

        public override string Render(ExpressionStatementSyntax exp) =>
            $"SELECT DISTINCT {m2mTable.OtherIdColumnName} FROM {m2mTable.TableName} WHERE";

        public override string Render(LiteralExpressionSyntax exp)
        {
            return exp.Kind() == SyntaxKind.StringLiteralExpression
                ? GroupNamePredicate(exp.ToString().Trim('\"'))
                : GroupIdPredicate(exp.ToString());
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
