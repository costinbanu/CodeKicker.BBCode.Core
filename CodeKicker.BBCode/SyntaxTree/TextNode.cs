using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Web;

namespace CodeKicker.BBCode.Core.SyntaxTree
{
    public sealed class TextNode : SyntaxTreeNode
    {
        public TextNode(string? text)
            : this(text, null)
        {
        }
        public TextNode(string? text, string? htmlTemplate)
            : base(null)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            HtmlTemplate = htmlTemplate;
        }

        public string Text { get; private set; }
        public string? HtmlTemplate { get; private set; }

        public override string ToHtml()
        {
            return (HtmlTemplate is null ? HttpUtility.HtmlEncode(Text) : HtmlTemplate.Replace("${content}", HttpUtility.HtmlEncode(Text))).Replace("\r", "");
        }
        public override string ToBBCode()
        {
            return Text.Replace("\\", "\\\\").Replace("[", "\\[").Replace("]", "\\]");
        }
        public override string ToLegacyBBCode() => HttpUtility.HtmlEncode(ToBBCode().Replace("\r", ""));
        public override string ToText()
        {
            return Text;
        }

        public override SyntaxTreeNode SetSubNodes(IEnumerable<SyntaxTreeNode> subNodes)
        {
            if (subNodes is null) throw new ArgumentNullException(nameof(subNodes));
            if (subNodes.Any()) throw new ArgumentException("subNodes cannot contain any nodes for a TextNode");
            return this;
        }
        internal override SyntaxTreeNode? AcceptVisitor(SyntaxTreeVisitor visitor)
        {
            if (visitor is null) throw new ArgumentNullException(nameof(visitor));
            return visitor.Visit(this);
        }

        protected override bool EqualsCore(SyntaxTreeNode b)
        {
            var casted = (TextNode)b;
            return Text == casted.Text && HtmlTemplate == casted.HtmlTemplate;
        }
    }
}