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
        public TagNode(BBTag tag)
            : this(tag, null)
        {
        }
        public TagNode(BBTag tag, IEnumerable<SyntaxTreeNode> subNodes)
            : base(subNodes)
        {
            Tag = tag ?? throw new ArgumentNullException("tag");
            AttributeValues = new Dictionary<BBAttribute, string>();
        }

        public BBTag Tag { get; private set; }
        public IDictionary<BBAttribute, string> AttributeValues { get; private set; }

        public override string ToHtml()
        {
            var content = GetContent();
            return ReplaceAttributeValues(Tag.OpenTagTemplate, content, false) + (Tag.AutoRenderContent ? content : null) + ReplaceAttributeValues(Tag.CloseTagTemplate, content, true);
        }
        public override string ToBBCode()
        {
            var content = string.Concat(SubNodes.Select(s => s.ToBBCode()).ToArray());

            var attrs = "";
            var defAttr = Tag.FindAttribute("");
            if (defAttr != null)
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
            var defAttr = Tag.FindAttribute("");
            var attachFlag = "";
            if (defAttr != null && AttributeValues.ContainsKey(defAttr))
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
                toReturn.Append(":").Append(Tag.BBCodeUid);
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
                trailingWhitespace = content.Substring(pos + 1);
            }

            toReturn.Append("]").Append(attachFlag).Append(nonEmptyContent.Replace("\r", "")).Append(attachFlag).Append("[/").Append(Tag.Name);

            if(!string.IsNullOrWhiteSpace(Tag.BBCodeUid))
            {
                toReturn.Append(":");
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
            toReturn.Append("]").Append(trailingWhitespace);
            return toReturn.ToString();
        }
        public override string ToText()
        {
            return string.Concat(SubNodes.Select(s => s.ToText()).ToArray());
        }

        string GetContent()
        {
            var content = string.Concat(SubNodes.Select(s => s.ToHtml()).ToArray());
            return Tag.ContentTransformer == null ? content : Tag.ContentTransformer(content);
        }

        string ReplaceAttributeValues(string template, string content, bool isClosingTag)
        {
            var attributesWithValues = (from attr in Tag.Attributes
                                        group attr by attr.ID into gAttrByID
                                        let val = (from attr in gAttrByID
                                                   let val = TryGetValue(attr)
                                                   where val != null
                                                   select new { attr, val }).FirstOrDefault()
                                        select new { attrID = gAttrByID.Key, attrAndVal = val }).ToList();

            var attrValuesByID = attributesWithValues.Where(x => x.attrAndVal != null).ToDictionary(x => x.attrID, x => x.attrAndVal.val);
            if (!attrValuesByID.ContainsKey(BBTag.ContentPlaceholderName))
                attrValuesByID.Add(BBTag.ContentPlaceholderName, content);

            var output = template;
            foreach (var x in attributesWithValues)
            {
                var placeholderStr = "${" + x.attrID + "}";

                if (x.attrAndVal != null)
                {
                    //replace attributes with values
                    var rawValue = x.attrAndVal.val;
                    var attribute = x.attrAndVal.attr;
                    output = ReplaceAttribute(output, attribute, rawValue, placeholderStr, attrValuesByID, isClosingTag);
                }
            }

            //replace empty attributes
            var attributeIDsWithValues = new HashSet<string>(attributesWithValues.Where(x => x.attrAndVal != null).Select(x => x.attrID));
            var emptyAttributes = Tag.Attributes.Where(attr => !attributeIDsWithValues.Contains(attr.ID)).ToList();
            
            foreach (var attr in emptyAttributes)
            {
                var placeholderStr = "${" + attr.ID + "}";
                output = ReplaceAttribute(output, attr, null, placeholderStr, attrValuesByID, isClosingTag);
            }

            output = output.Replace("${" + BBTag.ContentPlaceholderName + "}", content);
            return output;
        }

        static string ReplaceAttribute(string output, BBAttribute attribute, string rawValue, string placeholderStr, Dictionary<string, string> attrValuesByID, bool isClosingTag)
        {
            string effectiveValue;
            if (attribute.ContentTransformer == null)
            {
                effectiveValue = rawValue;
            }
            else
            {
                var ctx = new AttributeRenderingContextImpl(attribute, rawValue, attrValuesByID);
                effectiveValue = attribute.ContentTransformer(ctx);
            }

            if (effectiveValue == null) effectiveValue = "";

            var encodedValue =
                attribute.HtmlEncodingMode == HtmlEncodingMode.HtmlAttributeEncode ? HttpUtility.HtmlAttributeEncode(effectiveValue)
                    : attribute.HtmlEncodingMode == HtmlEncodingMode.HtmlEncode ? HttpUtility.HtmlEncode(effectiveValue)
                          : effectiveValue;
            output = output.Replace(placeholderStr, isClosingTag ? encodedValue.Split(' ')[0] : encodedValue);
            return output;
        }

        string TryGetValue(BBAttribute attr)
        {
            AttributeValues.TryGetValue(attr, out string val);
            return val;
        }

        public override SyntaxTreeNode SetSubNodes(IEnumerable<SyntaxTreeNode> subNodes)
        {
            if (subNodes == null) throw new ArgumentNullException("subNodes");
            return new TagNode(Tag, subNodes)
                {
                    AttributeValues = new Dictionary<BBAttribute, string>(AttributeValues),
                };
        }
        internal override SyntaxTreeNode AcceptVisitor(SyntaxTreeVisitor visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");
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
            public AttributeRenderingContextImpl(BBAttribute attribute, string attributeValue, IDictionary<string, string> getAttributeValueByIdData)
            {
                Attribute = attribute;
                AttributeValue = attributeValue;
                GetAttributeValueByIDData = getAttributeValueByIdData;
            }

            public BBAttribute Attribute { get; private set; }
            public string AttributeValue { get; private set; }
            public IDictionary<string, string> GetAttributeValueByIDData { get; private set; }

            public string GetAttributeValueByID(string id)
            {
                if (!GetAttributeValueByIDData.TryGetValue(id, out string value)) return null;
                return value;
            }
        }
    }
}
