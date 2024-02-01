using AngleSharp;
using AngleSharp.Dom;

namespace NoK
{
    public static class INodeExtensions
    {
        public static List<INode> ParseFragment(string? html)
        {
            if (html?.Any() == false)
                return new();
            var id = Guid.NewGuid().ToString();
            html = $"<div id={id}>{html}</div>";
            using var context = BrowsingContext.New(Configuration.Default);
            using var doc = context.OpenAsync(req => req.Content(html)).Result;
            var container = doc?.GetElementById(id);
            return container?.ChildNodes.ToList() ?? new();
        }

        public static void RemoveDescendants(this INode parent, IEnumerable<INode> nodes)
        {
            if (!nodes.Any())
                return;
            if (nodes.Contains(parent))
            {
                var children = parent.ChildNodes.Reverse().ToList();
                foreach (var item in children)
                    parent.RemoveChild(item);
                return;
            }
            var removed = new List<INode>();
            foreach (var item in nodes)
            {
                if (parent.ChildNodes.Contains(item))
                    removed.Add(parent.RemoveChild(item));
            }

            foreach (var child in parent.ChildNodes)
                child.RemoveDescendants(nodes.Except(removed));
        }


        public static INode CreateCopyUntilMatch(INode node, Func<INode, bool> match, bool checkSiblings = false)
        {
            var copy = node.Clone(false);
            if (checkSiblings && node.Parent != null)
            {
                var parent = node.Parent.Clone(false);
                parent.AppendChild(copy);
            }
            CopyUntilMatch(node, copy, match, checkSiblings);
            return copy;
        }

        public static bool CopyUntilMatch(INode node, INode copy, Func<INode, bool> match, bool checkSiblings = false)
        {
            if (match(node))
                return true;

            foreach (var item in node.ChildNodes)
            {
                if (match(node))
                    return true;
                var cc = item.Clone(false);
                copy.AppendChild(cc);
                if (CopyUntilMatch(item, cc, match))
                    return true;
            }

            if (checkSiblings && node.Parent != null && copy.Parent != null)
            {
                for (int i = node.Index() + 1; i < node.Parent.ChildNodes.Count(); i++)
                {
                    var item = node.Parent.ChildNodes[i];
                    if (match(item))
                        return true;
                    var cc = item.Clone(false);
                    copy.Parent!.AppendChild(cc);
                    if (CopyUntilMatch(item, cc, match))
                        return true;
                }
            }
            return false;
        }
    }
}