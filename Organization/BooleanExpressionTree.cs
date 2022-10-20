using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;

namespace Organization
{
    public class BooleanExpressionTree
    {
        public static void ParseExperiment(string input)
        {
            var tree = CSharpSyntaxTree.ParseText(input);
            if (tree == null)
                return;
            var root = tree.GetCompilationUnitRoot();
            if (root == null)
                return;

            var expressionRoot = root.DescendantNodes().FirstOrDefault(o => o is ExpressionStatementSyntax);
            if (expressionRoot == null)
                return;

            var list = new List<string>();
            Recurse(expressionRoot, list);
            //var list = Traverse(expressionRoot).ToList();

            void Recurse(SyntaxNode parent, List<string> output)
            {
                var indent = string.Join("", Enumerable.Range(0, parent.Ancestors().Count()).Select(o => " "));

                if (parent is LiteralExpressionSyntax literal)
                {
                    output.Add(literal.ToString());
                }
                else if (parent is IdentifierNameSyntax identifier)
                {
                    output.Add(identifier.ToString());
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
                    output.Add(binary.OperatorToken.ToString());
                    DoParentAndChildren(binary.Right);
                }
                else if (parent is PrefixUnaryExpressionSyntax unary)
                {
                    // Maybe only allow ! and -
                    output.Add(unary.OperatorToken.ToString());
                    DoChildren(parent);
                }
                else if (parent is ExpressionStatementSyntax)
                {
                    DoChildren(parent);
                }
                else
                    throw new Exception($"{parent.GetType().Name} {parent.Kind()} {parent.GetText()}");

                void DoParentAndChildren(SyntaxNode p)
                {
                    Recurse(p, output);
                }
                void DoChildren(SyntaxNode p) => p.ChildNodes().ToList().ForEach(o => Recurse(o, output));
            }
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

        //private static void XX()
        //{
        //    var q1 = "account_id {0} IN (SELECT account_id FROM accounts_groups WHERE";
        //    var subqId = q1 + "group_id = {1})";
        //    var subqName = q1 + "group_id IN (SELECT id FROM groups WHERE name = {1}))";
        //}

        private string GenerateAccountGroupQueryNoOuter(string q)
        {
            //TODO: recursively process NOT clauses (right now "... NOT (A AND B) doesn't work)
            //'Åk 1' AND ('Skola 1' OR 'Skola 2') NOT 'Low WM'
            //WHERE account_id IN ('åk 1')
            //AND   (account_id IN ('skola 1') OR account_id IN ('skola 2'))
            //AND   account_id  NOT IN ('low wm')
            var subqId = "account_id {0} IN (SELECT account_id FROM accounts_groups WHERE group_id = {1})";
            var subqName = "account_id {0} IN (SELECT account_id FROM accounts_groups WHERE group_id IN (SELECT id FROM groups WHERE name = {1}))";
            q = q.Trim();
            var result = "";
            var ms = Regex.Matches(q, @"('[\w\s-@,.:&/]+'|\d+)"); //'[\w\s]+'");
            var rxEndPara = new Regex(@"^\)+");
            var rxStartPara = new Regex(@"\(+$");
            var rxIsNonNumeric = new Regex(@"\D");
            int pos = 0;
            foreach (Match m in ms)
            {
                //TODO: check m.Value for SQL injection
                var upToHere = q.Substring(pos, m.Index - pos).Trim().ToUpper();
                //Add any parenthesis
                var parenthesis = rxEndPara.Match(upToHere);
                if (parenthesis.Success)
                {
                    result += parenthesis.Value;
                    upToHere = upToHere.Substring(parenthesis.Index + parenthesis.Length);
                }
                parenthesis = rxStartPara.Match(upToHere);
                if (parenthesis.Success)
                    upToHere = upToHere.Remove(parenthesis.Index);
                var tmp = upToHere.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (tmp.Length > 0)
                {
                    if (tmp[0] == "NOT")
                        tmp = new string[] { result.Length == 0 ? "" : "AND", "NOT" };
                    if (tmp[0] != "AND" && tmp[0] != "OR")
                        throw new Exception("AND or OR required after " + q.Substring(0, m.Index));
                    result += tmp[0];
                }
                else if (result.Length > 0)
                    throw new Exception("Missing AND/OR/NOT after " + q.Substring(0, m.Index));

                if (parenthesis.Success)
                    result += parenthesis.Value;

                var addNot = false;
                if (tmp.Length > 1)
                {
                    if (tmp[1] == "NOT")
                        addNot = true;
                    else
                        throw new Exception("");
                }
                var isNumeric = !rxIsNonNumeric.Match(m.Value).Success;
                result += " " + string.Format(isNumeric ? subqId : subqName, addNot ? "NOT" : "", m) + "\r\n";
                pos = m.Index + m.Length;
            }
            if (pos < q.Length)
            {
                var closingParenthesis = q.Substring(pos).Trim();
                if (!new Regex(@"[^\)]").IsMatch(closingParenthesis))
                    result += closingParenthesis;
            }
            return result;
        }
    }
}
