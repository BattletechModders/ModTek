using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ModTek.Features.Logging
{
/*
RT benchmark by loading into the game
    trie prefix groups as regex, (?:word(?:1|2))
        total=00:00:00.0618348
    regex without prefix groups, (?:word1|word2)
        total=00:00:00.1397503
    array indexOf
        total=00:00:00.5468723

1'000'000 on RT prefixes
    trieRegex.IsMatch, (?:word(?:1|2))
        00:00:00.5821893
    trie.Matches
        00:00:00.8223026
    regex.IsMatch, (?:word1|word2)
        00:00:01.8006954
*/
    internal class Trie
    {
        internal static Trie Create(IEnumerable<string> prefixes)
        {
            var trie = new Trie();
            var iter = prefixes
                .GroupBy(x => x).Select(y => y.First()) // remove duplicates
                .OrderBy(m => m) // order is required by the algorithm
                .Select(Regex.Escape); // escape stuff
            foreach (var prefix in iter)
            {
                trie.Add(prefix);
                // Console.WriteLine(trie.ToString());
            }
            return trie;
        }

        internal Regex CompileRegex()
        {
            var sb = new StringBuilder();
            var traverser = new Traverser
            {
                enteredNode = node => sb.Append(node.prefixPart),
                enteringFirstSibling = node => sb.Append("(?:"),
                enteringNextSibling = node => sb.Append("|"),
                exitedLastSibling = node => sb.Append(")")
            };
            Traverse(traverser);
            return new Regex(sb.ToString());
        }

        private class Traverser
        {
            internal Action<Node> enteredNode;
            internal Action<Node> enteringFirstSibling;
            internal Action<Node> enteringNextSibling;
            internal Action<Node> exitedLastSibling;
        }

        private void Traverse(Traverser traverser)
        {
            var node = root;
            while (node != null)
            {
                traverser.enteredNode(node);
                if (node.IsLeaf)
                {
                    var backtrackingNode = node;
                    while (true)
                    {
                        if (backtrackingNode.IsLastSibling)
                        {
                            traverser.exitedLastSibling(backtrackingNode);
                        }

                        var parent = backtrackingNode.parent;
                        if (parent == null)
                        {
                            node = null;
                            break;
                        }

                        var nextSibling = backtrackingNode.NextSibling();
                        if (nextSibling == null)
                        {
                            backtrackingNode = parent;
                        }
                        else
                        {
                            node = nextSibling;
                            traverser.enteringNextSibling(node);
                            break;
                        }
                    }
                }
                else
                {
                    if (node.ChildCount > 1)
                    {
                        traverser.enteringFirstSibling(node);
                    }
                    node = node.FirstChild;
                }
            }
        }

        // possible changes (none are important, logging and prefix matches are a very small part):
        // - slower than the compiled regex IsMatch, code probably could improve it a bit
        // - data structure could be made more compact, e.g. using less references and more value types, like continuous array(s)
        // - algorithm itself could be improved by including frequency with over time invalidation (even caching longer prefix parts to avoid repeated lookups, see Denormalization)
        // - rewriting the whole logging stuff to hook into Logger and do contains checks against separate LoggerName, LogLevel and LogMessage instead of prefix searches
        internal bool Matches(string text)
        {
            var node = root;
            var nodePrefixPartIndex = 0;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (nodePrefixPartIndex == node.prefixPart.Length) // end of part reached
                {
                    if (node != root && node.IsLeaf)
                    {
                        return true;
                    }

                    var found = false;
                    foreach (var child in node.children)
                    {
                        if (child.prefixPart[0] == c) // found a child matching, lets continue there
                        {
                            found = true;
                            node = child;
                            nodePrefixPartIndex = 0;
                            i--;
                            break;
                        }
                    }

                    if (!found)
                    {
                        return false;
                    }
                }
                else
                {
                    var partC = node.prefixPart[nodePrefixPartIndex];
                    if (c != partC)
                    {
                        return false;
                    }

                    nodePrefixPartIndex++;
                }
            }

            return false;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            ToString(sb, root);
            return sb.ToString();
        }

        private static void ToString(StringBuilder sb, Node node, int indent = 0)
        {
            var indentStr =  new string('\t', indent);
            sb.Append($"{indentStr}'{node.prefixPart}'\n");
            if (node.children != null)
            {
                indent++;
                foreach (var child in node.children)
                {
                    ToString(sb, child, indent);
                }
            }
        }

        private readonly Node root = new Node
        {
            children = new List<Node>()
        };

        private void Add(string prefix)
        {
            var node = root;
            var nodePrefixPartIndex = 0;

            for (var i = 0; i < prefix.Length; i++)
            {
                var c = prefix[i];
                if (nodePrefixPartIndex == node.prefixPart.Length) // end of part reached
                {
                    if (node != root && node.IsLeaf)
                    {
                        return; // no need to add, a prefix already exists
                    }

                    var found = false;
                    foreach (var child in node.children)
                    {
                        if (child.prefixPart[0] == c) // found a child matching, lets continue there
                        {
                            found = true;
                            node = child;
                            nodePrefixPartIndex = 0;
                            i--;
                            break;
                        }
                    }

                    if (!found)
                    {
                        var newNode = new Node();
                        newNode.prefixPart = prefix.Substring(i);
                        newNode.parent = node;
                        node.children.Add(newNode);
                        return;
                    }
                }
                else
                {
                    var partC = node.prefixPart[nodePrefixPartIndex];
                    if (c == partC)
                    {
                        nodePrefixPartIndex++;
                        continue;
                    }

                    var newNodeWithNewContent = new Node();
                    newNodeWithNewContent.prefixPart = prefix.Substring(i);

                    if (nodePrefixPartIndex == 0)
                    {
                        node.parent.children.Add(newNodeWithNewContent);
                    }
                    else
                    {
                        // split existing node

                        var newNodeWithExistingContent = new Node();
                        newNodeWithExistingContent.prefixPart = node.prefixPart.Substring(nodePrefixPartIndex);
                        newNodeWithExistingContent.children = node.children;
                        if (newNodeWithExistingContent.AnyChildren)
                        {
                            foreach (var child in newNodeWithExistingContent.children)
                            {
                                child.parent = newNodeWithExistingContent;
                            }
                        }

                        node.prefixPart = node.prefixPart.Substring(0, nodePrefixPartIndex);
                        node.children = new List<Node>();
                        node.children.Add(newNodeWithExistingContent);
                        newNodeWithExistingContent.parent = node;
                        node.children.Add(newNodeWithNewContent);
                        newNodeWithNewContent.parent = node;
                    }

                    return;
                }
            }

            // this could be reached if duplicates would be allowed
            throw new Exception();
        }

        internal class Node
        {
            internal string prefixPart = "";
            internal Node parent;
            internal List<Node> children;
            internal bool IsLeaf => !AnyChildren;
            internal Node FirstChild => AnyChildren ? children[0] : null;
            internal Node LastChild => AnyChildren ? children[children.Count - 1] : null;
            internal bool AnyChildren => ChildCount > 0;
            internal int ChildCount => children?.Count ?? 0;
            internal bool AnySiblings => parent != null && parent.children.Count > 1;
            internal bool IsLastSibling => AnySiblings && this == parent.LastChild;

            internal Node NextSibling()
            {
                if (parent == null)
                {
                    return null;
                }
                var siblings = parent.children;
                var index =  siblings.IndexOf(this);
                if (index < 0)
                {
                    throw new Exception();
                }
                var nextSiblingIndex = index + 1;
                if (nextSiblingIndex < parent.children.Count)
                {
                    return parent.children[nextSiblingIndex];
                }
                return null;
            }

            public override string ToString()
            {
                return prefixPart;
            }
        }
    }
}