using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Web;
using System.Text;

namespace CodeKicker.BBCode.Core.SyntaxTree
{
    public sealed class TagNode : SyntaxTreeNode
    {
        public TagNode(BBTag? tag)
            : this(tag, null)
        {
        }
        public TagNode(BBTag? tag, IEnumerable<SyntaxTreeNode>? subNodes)
            : base(subNodes)
        {
            Tag = tag ?? throw new ArgumentNullException(nameof(tag));
            AttributeValues = new Dictionary<BBAttribute, string>();
        }

        public BBTag Tag { get; private set; }
        public IDictionary<BBAttribute, string> AttributeValues { get; private set; }

        public override string ToHtml()
        {
            var content = GetContent();
            try
            {
                return ReplaceAttributeValues(Tag.OpenTagTemplate, content, false) + (Tag.AutoRenderContent ? content : null) + ReplaceAttributeValues(Tag.CloseTagTemplate, content, true);
            }
            catch
            {
                return content;
            }
        }
        public override string ToBBCode()
        {
            var content = string.Concat(SubNodes.Select(s => s.ToBBCode()).ToArray());

            var attrs = "";
            if (Tag.FindAttribute("", out var defAttr))
            {
                if (AttributeValues.ContainsKey(defAttr))
                    attrs += "=" + AttributeValues[defAttr];
            }
            foreach (var attrKvp in AttributeValues)
            {
                if (attrKvp.Key.Name == "") continue;
                attrs += " " + attrKvp.Key.Name + "=" + attrKvp.Value;
            }
            return "[" + Tag.Name + attrs + "]" + content + "[/" + Tag.Name + "]";
        }
        public override string ToLegacyBBCode()
        {
            var content = string.Concat(SubNodes.Select(s => s.ToLegacyBBCode()).ToArray());

            var attrs = "";
            var attachFlag = "";
            if (Tag.FindAttribute("", out var defAttr) && AttributeValues.ContainsKey(defAttr))
            {
                attrs += "=" + AttributeValues[defAttr];
            }
            foreach (var attrKvp in AttributeValues)
            {
                if (Tag.Name.Equals("attachment", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(attachFlag) && attrKvp.Key.ID.Equals("num", StringComparison.OrdinalIgnoreCase) && int.TryParse(attrKvp.Value, out var id))
                {
                    attachFlag = $"<!-- ia{id} -->";
                }

                if (attrKvp.Key.Name == "") continue;
                attrs += " " + attrKvp.Key.Name + "=" + attrKvp.Value;
            }

            var toReturn = new StringBuilder("[").Append(Tag.Name).Append(HttpUtility.HtmlEncode(attrs));
            
            if (!string.IsNullOrWhiteSpace(Tag.BBCodeUid))
            {
                toReturn.Append(':').Append(Tag.BBCodeUid);
            }

            string nonEmptyContent = content, trailingWhitespace = "";
            if (Tag.EnableIterationElementBehavior && !string.IsNullOrWhiteSpace(content) && char.IsWhiteSpace(content[^1]))
            {
                var pos = content.Length - 1;
                while(char.IsWhiteSpace(content[pos]) && pos > 0)
                {
                    pos--;
                }
                nonEmptyContent = content[0..(pos + 1)];
                trailingWhitespace = content[(pos + 1)..];
            }

            toReturn.Append(']').Append(attachFlag).Append(nonEmptyContent.Replace("\r", "")).Append(attachFlag).Append("[/").Append(Tag.Name);

            if(!string.IsNullOrWhiteSpace(Tag.BBCodeUid))
            {
                toReturn.Append(':');
                switch (true)
                {
                    case bool _ when Tag.Name.Equals("*", StringComparison.OrdinalIgnoreCase): 
                        toReturn.Append("m:"); 
                        break;
                    case bool _ when Tag.Name.Equals("list", StringComparison.OrdinalIgnoreCase) && AttributeValues.Any():
                        toReturn.Append("o:");
                        break;
                    case bool _ when Tag.Name.Equals("list", StringComparison.OrdinalIgnoreCase) && !AttributeValues.Any():
                        toReturn.Append("u:");
                        break;
                    default: break;
                }
                toReturn.Append(Tag.BBCodeUid);
            }
            toReturn.Append(']').Append(trailingWhitespace);
            return toReturn.ToString();
        }
        public override string ToText()
        {
            return string.Concat(SubNodes.Select(s => s.ToText()).ToArray());
        }

        string GetContent()
        {
            var content = string.Concat(SubNodes.Select(s => s.ToHtml()).ToArray());
            try
            {
                if (Tag.ContentTransformer is not null)
                {
                    content = Tag.ContentTransformer(content);
                }
            }
            catch { }
            return content;
        }

        string ReplaceAttributeValues(string template, string content, bool isClosingTag)
        {
            var attributesWithValues = (from attr in Tag.Attributes
                                        group attr by attr.ID into gAttrByID
                                        let val = (from attr in gAttrByID
                                                   let val = TryGetValue(attr)
                                                   where val is not null
                                                   select new { attr, val }).FirstOrDefault()
                                        select new { attrID = gAttrByID.Key, attrAndVal = val }).ToList();

            var attrValuesByID = attributesWithValues.Where(x => x.attrAndVal is not null).ToDictionary(x => x.attrID, x => x.attrAndVal.val);
            if (!attrValuesByID.ContainsKey(BBTag.ContentPlaceholderName))
                attrValuesByID.Add(BBTag.ContentPlaceholderName, content);

            var output = template;
            foreach (var x in attributesWithValues)
            {
                var placeholderStr = "${" + x.attrID + "}";

                if (x.attrAndVal is not null)
                {
                    //replace attributes with values
                    var rawValue = x.attrAndVal.val;
                    var attribute = x.attrAndVal.attr;
                    output = ReplaceAttribute(output, attribute, rawValue, placeholderStr, attrValuesByID, isClosingTag, content);
                }
            }

            //replace empty attributes
            var attributeIDsWithValues = new HashSet<string>(attributesWithValues.Where(x => x.attrAndVal is not null).Select(x => x.attrID));
            var emptyAttributes = Tag.Attributes.Where(attr => !attributeIDsWithValues.Contains(attr.ID)).ToList();
            
            foreach (var attr in emptyAttributes)
            {
                var placeholderStr = "${" + attr.ID + "}";
                output = ReplaceAttribute(output, attr, null, placeholderStr, attrValuesByID, isClosingTag, content);
            }

            output = output.Replace("${" + BBTag.ContentPlaceholderName + "}", content);
            return output;
        }

        static string ReplaceAttribute(string output, BBAttribute attribute, string? rawValue, string placeholderStr, Dictionary<string, string> attrValuesByID, bool isClosingTag, string tagContent)
        {
            string? effectiveValue;
            if (attribute.ContentTransformer is null)
            {
                effectiveValue = rawValue;
            }
            else
            {
                var ctx = new AttributeRenderingContextImpl(attribute, rawValue, attrValuesByID, tagContent);
                effectiveValue = attribute.ContentTransformer(ctx);
            }

            if (effectiveValue is null) effectiveValue = "";

            var encodedValue =
                attribute.HtmlEncodingMode == HtmlEncodingMode.HtmlAttributeEncode ? HttpUtility.HtmlAttributeEncode(effectiveValue)
                    : attribute.HtmlEncodingMode == HtmlEncodingMode.HtmlEncode ? HttpUtility.HtmlEncode(effectiveValue)
                          : effectiveValue;
            output = output.Replace(placeholderStr, isClosingTag ? encodedValue.Split(' ')[0] : encodedValue);
            return output;
        }

        string? TryGetValue(BBAttribute attr)
        {
            AttributeValues.TryGetValue(attr, out string? val);
            return val;
        }

        public override SyntaxTreeNode SetSubNodes(IEnumerable<SyntaxTreeNode> subNodes)
        {
            if (subNodes is null) throw new ArgumentNullException(nameof(subNodes));
            return new TagNode(Tag, subNodes)
                {
                    AttributeValues = new Dictionary<BBAttribute, string>(AttributeValues),
                };
        }
        internal override SyntaxTreeNode? AcceptVisitor(SyntaxTreeVisitor visitor)
        {
            if (visitor is null) throw new ArgumentNullException(nameof(visitor));
            return visitor.Visit(this);
        }

        protected override bool EqualsCore(SyntaxTreeNode b)
        {
            var casted = (TagNode)b;
            return
                Tag == casted.Tag &&
                AttributeValues.All(attr => casted.AttributeValues[attr.Key] == attr.Value) &&
                casted.AttributeValues.All(attr => AttributeValues[attr.Key] == attr.Value);
        }

        class AttributeRenderingContextImpl : IAttributeRenderingContext
        {
            public AttributeRenderingContextImpl(BBAttribute attribute, string? attributeValue, IDictionary<string, string> getAttributeValueByIdData, string tagContent)
            {
                Attribute = attribute;
                AttributeValue = attributeValue;
                GetAttributeValueByIDData = getAttributeValueByIdData;
                TagContent = tagContent;
            }

            public BBAttribute Attribute { get; }
            public string? AttributeValue { get; }
            public IDictionary<string, string> GetAttributeValueByIDData { get; }
            public string TagContent { get; }

            public string? GetAttributeValueByID(string id)
            {
                if (!GetAttributeValueByIDData.TryGetValue(id, out string? value)) return null;
                return value;
            }
        }
    }
}
