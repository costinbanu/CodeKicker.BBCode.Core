using CodeKicker.BBCode.Core.SyntaxTree;
using RandomTestValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CodeKicker.BBCode.Core.Tests
{
    public static class TestUtils
    {
        public static SequenceNode CreateRootNode(BBTag[] allowedTags)
        {
            var node = new SequenceNode();
            AddSubnodes(allowedTags, node);
            return node;
        }
        static SyntaxTreeNode CreateNode(BBTag[] allowedTags, bool allowText)
        {
            switch (new[] { allowText ? 0 : 1, 2 }[RandomValue.Int(1, 0)])
            {
                case 0:
                    var text = RandomValue.String();
                    return new TextNode(text);

                case 1:
                    var tag = allowedTags[RandomValue.Int(allowedTags.Length, 0)];
                    var node = new TagNode(tag);

                    AddSubnodes(allowedTags, node);

                    if (tag.Attributes is not null)
                    {
                        var selectedIds = new List<string>();
                        foreach (var attr in tag.Attributes)
                        {
                            if (!selectedIds.Contains(attr.ID) && RandomValue.Bool())
                            {
                                var specialChars = "[] ";
                                var val = new string($"{RandomValue.String()}{specialChars[RandomValue.Int(2, 0)]}".OrderBy(_ => RandomValue.Int()).ToArray());
                                node.AttributeValues[attr] = val;
                                selectedIds.Add(attr.ID);
                            }
                        }
                    }
                    return node;

                default:
                    throw new Exception("Fail!");
            }
        }
        
        static void AddSubnodes(BBTag[] allowedTags, SyntaxTreeNode node)
        {
            int count = RandomValue.Int(3, 0);
            bool lastWasText = false;
            for (int i = 0; i < count; i++)
            {
                var subNode = CreateNode(allowedTags, !lastWasText);
                lastWasText = subNode is TextNode;
                node.SubNodes.Add(subNode);
            }
        }

        public static BBCodeParser GetParserForTest(ErrorMode errorMode, bool includePlaceholder, BBTagClosingStyle listItemBBTagClosingStyle, bool enableIterationElementBehavior)
            => new BBCodeParser(errorMode, null, new[]
            {
                new BBTag("b", "<b>", "</b>", 1), 
                new BBTag("i", "<span style=\"font-style:italic;\">", "</span>", 2), 
                new BBTag("u", "<span style=\"text-decoration:underline;\">", "</span>", 7), 
                new BBTag("code", "<pre style=\"font-family: ui-monospace;\">", "</pre>", 8), 
                new BBTag("img", "<img src=\"${content}\" />", "", false, true, 4), 
                new BBTag("quote", "<blockquote>", "</blockquote>", 0), 
                new BBTag("list", "<ul>", "</ul>", 9), 
                new BBTag("*", "<li>", "</li>", true, listItemBBTagClosingStyle, null, enableIterationElementBehavior, 13), 
                new BBTag("url", "<a href=\"${href}\">", "</a>", 3, "", false, new BBAttribute("href", ""), new BBAttribute("href", "href")), 
                new BBTag("url2", "<a href=\"${href}\">", "</a>", 14, "", false, new BBAttribute("href", "", GetUrl2Href), new BBAttribute("href", "href", GetUrl2Href)), 
                !includePlaceholder ? null! : new BBTag("placeholder", "${name}", "", false, BBTagClosingStyle.LeafElementWithoutContent, null, 15, "", true, new BBAttribute("name", "", name => "xxx" + name.AttributeValue + "yyy")), 
            }.Where(x => x is not null).ToList());

        public static BBCodeParser GetCustomParser()
        {
            string urlTransformer (string url)
            {
                if (!url.StartsWith("www", StringComparison.InvariantCultureIgnoreCase) && !url.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new ArgumentException("Bad URL formatting");
                }
                else if (url.StartsWith("www", StringComparison.InvariantCultureIgnoreCase))
                {
                    url = $"//{url}";
                }
                return url;
            }

            return new BBCodeParser(new[]
            {
                    new BBTag("b", "<b>", "</b>", 1),
                    new BBTag("i", "<i>", "</i>", 2),
                    new BBTag("u", "<u>", "</u>", 7),
                    new BBTag("code", "<pre style=\"font-family: ui-monospace;\">", "</pre>", 8, allowUrlProcessingAsText: false),
                    new BBTag("img", "<br/><img src=\"${content}\" /><br/>", "", false, BBTagClosingStyle.RequiresClosingTag, x => urlTransformer(x), false, 4, allowUrlProcessingAsText: false),
                    new BBTag("quote", "<blockquote class=\"PostQuote\">${name}", "</blockquote>", 0, "", true,
                        new BBAttribute("name", "", (a) => string.IsNullOrWhiteSpace(a.AttributeValue) ? "" : $"<b>{HttpUtility.HtmlDecode(a.AttributeValue).Trim('"')}</b> wrote:<br/>", HtmlEncodingMode.UnsafeDontEncode)) { GreedyAttributeProcessing = true },
                    new BBTag("*", "<li>", "</li>", true, BBTagClosingStyle.AutoCloseElement, null, true, 13),
                    new BBTag("list", "<${attr}>", "</${attr}>", true, true, 9, "", true,
                        new BBAttribute("attr", "", a => string.IsNullOrWhiteSpace(a.AttributeValue) ? "ul" : $"ol type=\"{a.AttributeValue}\"")),
                    new BBTag("url", "<a href=\"${href}\" target=\"_blank\">", "</a>", 3, "", false,
                        new BBAttribute("href", "", a => urlTransformer(string.IsNullOrWhiteSpace(a?.AttributeValue) ? a!.TagContent! : a.AttributeValue!), HtmlEncodingMode.UnsafeDontEncode)),
                    new BBTag("color", "<span style=\"color:${code}\">", "</span>", 6, "", true,
                        new BBAttribute("code", "")),
                    new BBTag("size", "<span style=\"font-size:${fsize}\">", "</span>", 5, "", true,
                        new BBAttribute("fsize", "", a => decimal.TryParse(a?.AttributeValue, out var val) ? FormattableString.Invariant($"{val / 100m:#.##}em") : "1em")),
                    new BBTag("attachment", "#{AttachmentFileName=${content}/AttachmentIndex=${num}}#", "", false, true, 12, "", true,
                        new BBAttribute("num", ""))
            });
        }

        static string? GetUrl2Href(IAttributeRenderingContext attributeRenderingContext)
        {
            if (!string.IsNullOrEmpty(attributeRenderingContext.AttributeValue)) return attributeRenderingContext.AttributeValue;

            var content = attributeRenderingContext.GetAttributeValueByID(BBTag.ContentPlaceholderName);
            if (!string.IsNullOrEmpty(content) && content.StartsWith("http:")) return content;

            return null;
        }

        public static BBCodeParser GetSimpleParserForTest(ErrorMode errorMode)
            => new BBCodeParser(errorMode, null, new[]
            {
                new BBTag("x", "${content}${x}", "${y}", true, true, 1, "", true, new BBAttribute("x", "x"), new BBAttribute("y", "y", x => x.AttributeValue!)), 
            });

        public static string SimpleBBEncodeForTest(string bbCode, ErrorMode errorMode)
        => GetSimpleParserForTest(errorMode).ToHtml(bbCode);

        public static bool IsValid(string bbCode, ErrorMode errorMode)
        {
            try
            {
                BBEncodeForTest(bbCode, errorMode);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static SequenceNode GetAnyTree()
        {
            var parser = GetParserForTest(RandomValue.Object<ErrorMode>(), true, RandomValue.Object<BBTagClosingStyle>(), false);
            return CreateRootNode(parser.Tags.ToArray());
        }

        public static string BBEncodeForTest(string bbCode, ErrorMode errorMode)
            => BBEncodeForTest(bbCode, errorMode, BBTagClosingStyle.AutoCloseElement, false);

        public static string BBEncodeForTest(string bbCode, ErrorMode errorMode, BBTagClosingStyle listItemBbTagClosingStyle, bool enableIterationElementBehavior)
            => GetParserForTest(errorMode, true, listItemBbTagClosingStyle, enableIterationElementBehavior).ToHtml(bbCode).Replace("\r", "").Replace("\n", "<br/>");
    }
}