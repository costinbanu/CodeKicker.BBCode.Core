using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CodeKicker.BBCode.Core.SyntaxTree
{
    public sealed class SequenceNode : SyntaxTreeNode
    {

        static readonly Regex LISTITEM_NEWLINE_REGEX = new Regex(@"\n?\<\/li\>\n?", RegexOptions.Compiled, TimeSpan.FromSeconds(20));

        public SequenceNode()
        {
        }
        public SequenceNode(SyntaxTreeNodeCollection subNodes)
            : base(subNodes)
        {
            if (subNodes == null) throw new ArgumentNullException("subNodes");
        }
        public SequenceNode(IEnumerable<SyntaxTreeNode> subNodes)
            : base(subNodes)
        {
            if (subNodes == null) throw new ArgumentNullException("subNodes");
        }

        public override string ToHtml()
        {
            var text = string.Concat(SubNodes.Select(s => s.ToHtml()).ToArray()).Replace("\r", "");
            var leadingNewLines = 0;
            var trailingNewLines = 0;

            for (var i = 0; i < text.Length && char.IsWhiteSpace(text[i]); i++)
            {
                if (text[i] == '\n')
                {
                    leadingNewLines++;
                }
            }
            for (var i = text.Length - 1; i >= 0 && char.IsWhiteSpace(text[i]); i--)
            {
                if (text[i] == '\n')
                {
                    trailingNewLines++;
                }
            }

            text = LISTITEM_NEWLINE_REGEX.Replace(text, "</li>");
            return (leadingNewLines > 1 ? "<br/>" : "") + text.Trim('\n').Replace("\n", "<br/>") + (trailingNewLines > 1 && trailingNewLines < text.Length ? "<br/>" : "");
        }
        public override string ToBBCode()
        {
            return string.Concat(SubNodes.Select(s => s.ToBBCode()).ToArray());
        }
        public override string ToLegacyBBCode()
        {
            return string.Concat(SubNodes.Select(s => s.ToLegacyBBCode()).ToArray());
        }
        public override string ToText()
        {
            return string.Concat(SubNodes.Select(s => s.ToText()).ToArray());
        }

        public override SyntaxTreeNode SetSubNodes(IEnumerable<SyntaxTreeNode> subNodes)
        {
            if (subNodes == null) throw new ArgumentNullException("subNodes");
            return new SequenceNode(subNodes);
        }
        internal override SyntaxTreeNode AcceptVisitor(SyntaxTreeVisitor visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");
            return visitor.Visit(this);
        }
        protected override bool EqualsCore(SyntaxTreeNode b)
        {
            return true;
        }
    }
}
