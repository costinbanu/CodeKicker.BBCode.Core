using CodeKicker.BBCode.Core.SyntaxTree;
using RandomTestValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Xunit;

namespace CodeKicker.BBCode.Core.Tests
{
    public partial class BBCodeParserTest
    {
        [Theory]
        [InlineData("", "")]
        [InlineData("a", "a")]
        [InlineData("a\r\nb", "a<br/>b")]
        [InlineData("[b]a\r\nb[/b]", "<b>a<br/>b</b>")]
        [InlineData("a\r\n[b]b[/b]", "a<br/><b>b</b>")]
        [InlineData("\r\n\r\n\r\na", "<br/>a")]
        [InlineData("a\r\n\r\n\r\n", "a<br/>")]
        [InlineData("a\r\n\r\n\r\nb", "a<br/><br/><br/>b")]
        [InlineData(" a b c ", " a b c ")]
        [InlineData("   a b c   ", "   a b c   ")]
        [InlineData("[b][/b]", "<b></b>")]
        [InlineData("text[b]text[/b]text", "text<b>text</b>text")]
        [InlineData("\r\n\r\n\r\n[b]text[/b]text\r\n\r\n\r\n", "<br/><b>text</b>text<br/>")]
        public void Whitespace_WhenCommonTagsIsCorrect(string input, string expected)
        {
            Assert.Equal(expected, BBEncodeForTest(input, ErrorMode.Strict));
        }

        [Theory]
        [InlineData("[list][*]aaa\r\n[*]bbb[/list]", "<ul><li>aaa</li><li>bbb</li></ul>")]
        [InlineData("[list]\r\n[*]aaa\r\n[*]bbb\r\n[/list]", "<ul><li>aaa</li><li>bbb</li></ul>")]
        [InlineData("[list]\n[*]aaa\n[*]bbb\n[/list]", "<ul><li>aaa</li><li>bbb</li></ul>")]
        [InlineData("[list]\r\n[*]one[/*]\r\n[*]two[/*]\r\n[/list]\r\n\r\n[list]\r\n[*]one\r\n[list]\r\n[*]a[/*]\r\n[*]b[/*]\r\n[/list][/*]\r\n[*]two[/*]\r\n[*]three[/*]\r\n[/list]",
            "<ul><li>one</li><li>two</li></ul><br/><br/><ul><li>one<br/><ul><li>a</li><li>b</li></ul></li><li>two</li><li>three</li></ul>")]
        [InlineData("[list]\n[*]one[/*]\n[*]two[/*]\n[/list]\n\n[list]\n[*]one\n[list]\n[*]a[/*]\n[*]b[/*]\n[/list][/*]\n[*]two[/*]\n[*]three[/*]\n[/list]",
            "<ul><li>one</li><li>two</li></ul><br/><br/><ul><li>one<br/><ul><li>a</li><li>b</li></ul></li><li>two</li><li>three</li></ul>")]
        public void Newline_ListItem_IsCorrect(string input, string expected)
        {
            var bbcodes = new List<BBTag>
            {
                new BBTag("*", "<li>", "</li>", true, BBTagClosingStyle.AutoCloseElement, x => x, true, 20),
                new BBTag("list", "<${attr}>", "</${attr}>", true, true, 9, "",
                    new BBAttribute("attr", "", a => string.IsNullOrWhiteSpace(a.AttributeValue) ? "ul" : $"ol type=\"{a.AttributeValue}\""))
            };
            var parser = new BBCodeParser(bbcodes);
            Assert.Equal(expected, parser.ToHtml(input));
        }

        [Fact]
        public void Url_IsCorrect()
        {
            Assert.Equal("<a href=\"http://example.org/path?name=value\">text</a>", BBEncodeForTest("[url=http://example.org/path?name=value]text[/url]", ErrorMode.Strict));
        }

        [Fact]
        public void LeafElementWithoutContent()
        {
            Assert.Equal("xxxnameyyy", BBEncodeForTest("[placeholder=name]", ErrorMode.Strict));
            Assert.Equal("xxxyyy", BBEncodeForTest("[placeholder=]", ErrorMode.Strict));
            Assert.Equal("xxxyyy", BBEncodeForTest("[placeholder]", ErrorMode.Strict));
            Assert.Equal("axxxyyyb", BBEncodeForTest("a[placeholder]b", ErrorMode.Strict));
            Assert.Equal("<b>a</b>xxxyyy<b>b</b>", BBEncodeForTest("[b]a[/b][placeholder][b]b[/b]", ErrorMode.Strict));
            Assert.Throws<BBCodeParsingException>(() => BBEncodeForTest("[placeholder][/placeholder]", ErrorMode.Strict));
            Assert.Throws<BBCodeParsingException>(() => BBEncodeForTest("[placeholder/]", ErrorMode.Strict));
        }

        [Fact]
        public void ImgTagHasNoContent()
        {
            Assert.Equal("<img src=\"url\" />", BBEncodeForTest("[img]url[/img]", ErrorMode.Strict));
        }

        [Fact]
        public void ListItemIsAutoClosed()
        {
            Assert.Equal("<li>item</li>", BBEncodeForTest("[*]item", ErrorMode.Strict, BBTagClosingStyle.AutoCloseElement, false));
            Assert.Equal("<ul><li>item</li></ul>", BBEncodeForTest("[list][*]item[/list]", ErrorMode.Strict, BBTagClosingStyle.AutoCloseElement, false));
            Assert.Equal("<li>item</li>", BBEncodeForTest("[*]item[/*]", ErrorMode.Strict, BBTagClosingStyle.AutoCloseElement, false));
            Assert.Equal("<li><li>item</li></li>", BBEncodeForTest("[*][*]item", ErrorMode.Strict, BBTagClosingStyle.AutoCloseElement, false));
            Assert.Equal("<li>1<li>2</li></li>", BBEncodeForTest("[*]1[*]2", ErrorMode.Strict, BBTagClosingStyle.AutoCloseElement, false));

            Assert.Equal("<li></li>item", BBEncodeForTest("[*]item", ErrorMode.Strict, BBTagClosingStyle.LeafElementWithoutContent, false));
            Assert.Equal("<ul><li></li>item</ul>", BBEncodeForTest("[list][*]item[/list]", ErrorMode.Strict, BBTagClosingStyle.LeafElementWithoutContent, false));
            Assert.Equal("<li></li><li></li>item", BBEncodeForTest("[*][*]item", ErrorMode.Strict, BBTagClosingStyle.LeafElementWithoutContent, false));
            Assert.Equal("<li></li>1<li></li>2", BBEncodeForTest("[*]1[*]2", ErrorMode.Strict, BBTagClosingStyle.LeafElementWithoutContent, false));

            Assert.Equal("<li>item</li>", BBEncodeForTest("[*]item", ErrorMode.Strict, BBTagClosingStyle.AutoCloseElement, true));
            Assert.Equal("<ul><li>item</li></ul>", BBEncodeForTest("[list][*]item[/list]", ErrorMode.Strict, BBTagClosingStyle.AutoCloseElement, true));
            Assert.Equal("<li>item</li>", BBEncodeForTest("[*]item[/*]", ErrorMode.Strict, BBTagClosingStyle.AutoCloseElement, true));
            Assert.Equal("<li></li><li>item</li>", BBEncodeForTest("[*][*]item", ErrorMode.Strict, BBTagClosingStyle.AutoCloseElement, true));
            Assert.Equal("<li>1</li><li>2</li>", BBEncodeForTest("[*]1[*]2", ErrorMode.Strict, BBTagClosingStyle.AutoCloseElement, true));
            Assert.Equal("<li>1<b>a</b></li><li>2</li>", BBEncodeForTest("[*]1[b]a[/b][*]2", ErrorMode.Strict, BBTagClosingStyle.AutoCloseElement, true));
            Assert.Equal("<li>1<b>a<li>2</li></b></li>", BBEncodeForTest("[*]1[b]a[*]2", ErrorMode.ErrorFree, BBTagClosingStyle.AutoCloseElement, true));
            Assert.Throws<BBCodeParsingException>(() => BBEncodeForTest("[*]1[b]a[*]2", ErrorMode.Strict, BBTagClosingStyle.AutoCloseElement, true));
        }

        [Theory]
        [InlineData("[list=1][*]one[list][*]bullet1[*]bullet2[/list][*]two[/list]", "<ol type=\"1\"><li>one<ul><li>bullet1</li><li>bullet2</li></ul></li><li>two</li></ol>")]
        [InlineData("[list][*]one[list][*]bullet1[*]bullet2[/list][*]two[/list]", "<ul><li>one<ul><li>bullet1</li><li>bullet2</li></ul></li><li>two</li></ul>")]
        [InlineData("[list=1][*]one[*][list][*]bullet1[*]bullet2[/list][*]two[/list]", "<ol type=\"1\"><li>one</li><li><ul><li>bullet1</li><li>bullet2</li></ul></li><li>two</li></ol>")]
        public void NestedLists_AreCorrect(string bbcode, string html)
        {
            var parser = new BBCodeParser(new List<BBTag>
            {
                    new BBTag("*", "<li>", "</li>", true, BBTagClosingStyle.AutoCloseElement, null, true, 20),
                    new BBTag("list", "<${attr}>", "</${attr}>", true, BBTagClosingStyle.RequiresClosingTag, null, 9, "",
                        new BBAttribute("attr", "", a => string.IsNullOrWhiteSpace(a.AttributeValue) ? "ul" : $"ol type=\"{a.AttributeValue}\""))
            });
            Assert.Equal(html, HttpUtility.HtmlDecode(parser.ToHtml(bbcode)));
        }

        [Fact]
        public void IgnoreNewlineAfterListItemTag()
        {
            Assert.Equal("<ul><li>item1</li><li>item2</li></ul>", BBEncodeForTest("[list]\r\n[*]item1\r\n[*]item2\r\n[/list]", ErrorMode.Strict, BBTagClosingStyle.AutoCloseElement, true));
        }

        [Fact]
        public void TagContentTransformer()
        {
            var parser = new BBCodeParser(new[]
                {
                    new BBTag("b", "<b>", "</b>", true, true, content => content.Trim(), 1),
                });

            Assert.Equal("<b>abc</b>", parser.ToHtml("[b] abc [/b]"));
        }

        [Fact]
        public void AttributeValueTransformer()
        {
            var parser = new BBCodeParser(ErrorMode.Strict, null, new[]
                {
                    new BBTag("font", "<span style=\"${color}${font}\">", "</span>", true, true, 1, "",
                        new BBAttribute("color", "color", attributeRenderingContext => string.IsNullOrEmpty(attributeRenderingContext.AttributeValue) ? "" : "color:" + attributeRenderingContext.AttributeValue + ";"),
                        new BBAttribute("font", "font", attributeRenderingContext => string.IsNullOrEmpty(attributeRenderingContext.AttributeValue) ? "" : "font-family:" + attributeRenderingContext.AttributeValue + ";")),
                });

            Assert.Equal("<span style=\"color:red;font-family:Arial;\">abc</span>", parser.ToHtml("[font color=red font=Arial]abc[/font]"));
            Assert.Equal("<span style=\"color:red;\">abc</span>", parser.ToHtml("[font color=red]abc[/font]"));
        }

        //the parser may never ever throw an exception other that BBCodeParsingException for any non-null input
        [Fact]
        public void NoCrash()
        {
            var errorMode = RandomValue.Object<ErrorMode>();
            var input = RandomValue.String();
            var listItemBbTagClosingStyle = RandomValue.Object<BBTagClosingStyle>();
            try
            {
                var output = BBEncodeForTest(input, errorMode, listItemBbTagClosingStyle, false);
                Assert.NotNull(output);
            }
            catch (BBCodeParsingException)
            {
                Assert.NotEqual(ErrorMode.ErrorFree, errorMode);
            }
        }

        [Fact]
        public void ErrorFreeModeAlwaysSucceeds()
        {
            BBEncodeForTest(RandomValue.String(), ErrorMode.ErrorFree);
        }

        //no script-tags may be contained in the output under any circumstances
        [Fact]
        public void NoScript_AnyInput()
        {
            var output = BBEncodeForTest(RandomValue.String(), RandomValue.Object<ErrorMode>());
            Assert.True(!output.Contains("<script"));
        }

        //no script-tags may be contained in the output under any circumstances
        [Fact]
        public void NoScript_AnyInput_Tree()
        {
            var parser = BBCodeTestUtil.GetParserForTest(ErrorMode.ErrorFree, true, BBTagClosingStyle.AutoCloseElement, false);
            var tree = BBCodeTestUtil.CreateRootNode(parser.Tags.ToArray());
            var output = tree.ToHtml();
            Assert.True(!output.Contains("<script"));
        }

        //no html-chars may be contained in the output under any circumstances
        [Fact]
        public void NoHtmlChars_AnyInput()
        {
            var output = BBCodeTestUtil.SimpleBBEncodeForTest(RandomValue.String(), RandomValue.Object<ErrorMode>());
            Assert.True(output.IndexOf('<') == -1);
            Assert.True(output.IndexOf('>') == -1);
        }

        [Fact]
        public void NoScript_FixedInput()
        {
            Assert.DoesNotContain("<script", BBEncodeForTest("<script>", RandomValue.Object<ErrorMode>()));
        }

        [Fact]
        public void NoScriptInAttributeValue()
        {
            Assert.DoesNotContain("<script", BBEncodeForTest("[url=<script>][/url]", RandomValue.Object<ErrorMode>()));
        }

        //1. given a syntax tree, encode it in BBCode, parse it back into a second syntax tree and ensure that both are exactly equal
        //2. given any syntax tree, the BBCode it represents must be parsable without error
        [Fact]
        public void Roundtrip()
        {
            var parser = BBCodeTestUtil.GetParserForTest(RandomValue.Object<ErrorMode>(), false, BBTagClosingStyle.AutoCloseElement, false);
            var tree = BBCodeTestUtil.CreateRootNode(parser.Tags.ToArray());
            var bbcode = tree.ToBBCode();
            var tree2 = parser.ParseSyntaxTree(bbcode);
            Assert.True(tree == tree2);
        }

        //given a BBCode-string, parse it into a syntax tree, encode the tree in BBCode, parse it back into a second sytax tree and ensure that both are exactly equal
        [Fact]
        public void Roundtrip2()
        {
            var parser = BBCodeTestUtil.GetParserForTest(RandomValue.Object<ErrorMode>(), false, BBTagClosingStyle.AutoCloseElement, false);
            SequenceNode tree;
            try
            {
                tree = parser.ParseSyntaxTree(RandomValue.String());
            }
            catch (BBCodeParsingException)
            {
                tree = null;
            }

            var bbcode = tree.ToBBCode();
            var tree2 = parser.ParseSyntaxTree(bbcode);
            Assert.True(tree == tree2);
        }

        [Fact]
        public void TextNodesCannotBeSplit()
        {
            var parser = BBCodeTestUtil.GetParserForTest(RandomValue.Object<ErrorMode>(), true, BBTagClosingStyle.AutoCloseElement, false);
            SequenceNode tree;
            try
            {
                tree = parser.ParseSyntaxTree(RandomValue.String());
            }
            catch (BBCodeParsingException)
            {
                return;
            }

            AssertTextNodesNotSplit(tree);
        }

        static void AssertTextNodesNotSplit(SyntaxTreeNode node)
        {
            if (node.SubNodes != null)
            {
                SyntaxTreeNode lastNode = null;
                for (int i = 0; i < node.SubNodes.Count; i++)
                {
                    AssertTextNodesNotSplit(node.SubNodes[i]);
                    if (lastNode != null)
                        Assert.False(lastNode is TextNode && node.SubNodes[i] is TextNode);
                    lastNode = node.SubNodes[i];
                }
            }
        }

        public static string BBEncodeForTest(string bbCode, ErrorMode errorMode)
        {
            return BBEncodeForTest(bbCode, errorMode, BBTagClosingStyle.AutoCloseElement, false);
        }
        public static string BBEncodeForTest(string bbCode, ErrorMode errorMode, BBTagClosingStyle listItemBbTagClosingStyle, bool enableIterationElementBehavior)
        {
            return BBCodeTestUtil.GetParserForTest(errorMode, true, listItemBbTagClosingStyle, enableIterationElementBehavior).ToHtml(bbCode).Replace("\r", "").Replace("\n", "<br/>");
        }

        [Fact]
        public void ToTextDoesNotCrash()
        {
            var input = RandomValue.String();
            var parser = BBCodeTestUtil.GetParserForTest(ErrorMode.ErrorFree, true, BBTagClosingStyle.AutoCloseElement, false);
            var text = parser.ParseSyntaxTree(input).ToText();
            Assert.True(text.Length <= input.Length);
        }

        [Fact]
        public void StrictErrorMode()
        {
            Assert.True(BBCodeTestUtil.IsValid(@"", ErrorMode.Strict));
            Assert.True(BBCodeTestUtil.IsValid(@"[b]abc[/b]", ErrorMode.Strict));
            Assert.False(BBCodeTestUtil.IsValid(@"[b]abc", ErrorMode.Strict));
            Assert.False(BBCodeTestUtil.IsValid(@"abc[0]def", ErrorMode.Strict));
            Assert.False(BBCodeTestUtil.IsValid(@"\", ErrorMode.Strict));
            Assert.False(BBCodeTestUtil.IsValid(@"\x", ErrorMode.Strict));
            Assert.False(BBCodeTestUtil.IsValid(@"[", ErrorMode.Strict));
            Assert.False(BBCodeTestUtil.IsValid(@"]", ErrorMode.Strict));
        }

        [Fact]
        public void CorrectingErrorMode()
        {
            Assert.True(BBCodeTestUtil.IsValid(@"", ErrorMode.TryErrorCorrection));
            Assert.True(BBCodeTestUtil.IsValid(@"[b]abc[/b]", ErrorMode.TryErrorCorrection));
            Assert.True(BBCodeTestUtil.IsValid(@"[b]abc", ErrorMode.TryErrorCorrection));

            Assert.Equal(@"\", BBEncodeForTest(@"\", ErrorMode.TryErrorCorrection));
            Assert.Equal(@"\x", BBEncodeForTest(@"\x", ErrorMode.TryErrorCorrection));
            Assert.Equal(@"\", BBEncodeForTest(@"\\", ErrorMode.TryErrorCorrection));
        }

        [Fact]
        public void CorrectingErrorMode_EscapeCharsIgnored()
        {
            Assert.Equal(@"\\", BBEncodeForTest(@"\\\\", ErrorMode.TryErrorCorrection));
            Assert.Equal(@"\", BBEncodeForTest(@"\", ErrorMode.TryErrorCorrection));
            Assert.Equal(@"\x", BBEncodeForTest(@"\x", ErrorMode.TryErrorCorrection));
            Assert.Equal(@"\", BBEncodeForTest(@"\\", ErrorMode.TryErrorCorrection));
            Assert.Equal(@"[", BBEncodeForTest(@"\[", ErrorMode.TryErrorCorrection));
            Assert.Equal(@"]", BBEncodeForTest(@"\]", ErrorMode.TryErrorCorrection));
        }

        [Fact]
        public void TextNodeHtmlTemplate()
        {
            var parserNull = new BBCodeParser(ErrorMode.Strict, null, new[]
                {
                    new BBTag("b", "<b>", "</b>", 1),
                });
            var parserEmpty = new BBCodeParser(ErrorMode.Strict, "", new[]
                {
                    new BBTag("b", "<b>", "</b>", 1),
                });
            var parserDiv = new BBCodeParser(ErrorMode.Strict, "<div>${content}</div>", new[]
                {
                    new BBTag("b", "<b>", "</b>", 1),
                });

            Assert.Equal(@"", parserNull.ToHtml(@""));
            Assert.Equal(@"abc", parserNull.ToHtml(@"abc"));
            Assert.Equal(@"abc<b>def</b>", parserNull.ToHtml(@"abc[b]def[/b]"));

            Assert.Equal(@"", parserEmpty.ToHtml(@""));
            Assert.Equal(@"", parserEmpty.ToHtml(@"abc"));
            Assert.Equal(@"<b></b>", parserEmpty.ToHtml(@"abc[b]def[/b]"));

            Assert.Equal(@"", parserDiv.ToHtml(@""));
            Assert.Equal(@"<div>abc</div>", parserDiv.ToHtml(@"abc"));
            Assert.Equal(@"<div>abc</div><b><div>def</div></b>", parserDiv.ToHtml(@"abc[b]def[/b]"));
        }

        [Fact]
        public void ContentTransformer_EmptyAttribute_CanChooseValueFromAttributeRenderingContext()
        {
            var parser = BBCodeTestUtil.GetParserForTest(ErrorMode.Strict, true, BBTagClosingStyle.AutoCloseElement, false);

            Assert.Equal(@"<a href=""http://codekicker.de"">http://codekicker.de</a>", parser.ToHtml(@"[url2]http://codekicker.de[/url2]"));
            Assert.Equal(@"<a href=""http://codekicker.de"">http://codekicker.de</a>", parser.ToHtml(@"[url2=http://codekicker.de]http://codekicker.de[/url2]"));
        }

        [Fact]
        public void StopProcessingDirective_StopsParserProcessingTagLikeText_UntilClosingTag()
        {
            var parser = new BBCodeParser(ErrorMode.ErrorFree, null, new[] { new BBTag("code", "<pre>", "</pre>", 1) { StopProcessing = true } });

            var input = "[code][i]This should [u]be a[/u] text literal[/i][/code]";
            var expected = "<pre>[i]This should [u]be a[/u] text literal[/i]</pre>";

            Assert.Equal(expected, parser.ToHtml(input));
        }

        [Fact]
        public void GreedyAttributeProcessing_ConsumesAllTokensForAttributeValue()
        {
            var parser = new BBCodeParser(ErrorMode.ErrorFree, null, new[] { new BBTag("quote", "<div><span>Posted by ${name}</span>", "</div>", 1, "", new BBAttribute("name", "")) { GreedyAttributeProcessing = true } });

            var input = "[quote=Test User With Spaces]Here is my comment[/quote]";
            var expected = "<div><span>Posted by Test User With Spaces</span>Here is my comment</div>";

            Assert.Equal(expected, parser.ToHtml(input));
        }

        [Fact]
        public void NewlineTrailingOpeningTagIsIgnored()
        {
            var parser = new BBCodeParser(ErrorMode.ErrorFree, null, new[] { new BBTag("code", "<pre>", "</pre>", 1) });

            var input = "[code]\nHere is some code[/code]";
            var expected = "<pre>Here is some code</pre>"; // No newline after the opening PRE

            Assert.Equal(expected, parser.ToHtml(input));
        }

        [Fact]
        public void SuppressFirstNewlineAfter_StopsFirstNewlineAfterClosingTag()
        {
            var parser = new BBCodeParser(ErrorMode.ErrorFree, null, new[] { new BBTag("code", "<pre>", "</pre>", 1) { SuppressFirstNewlineAfter = true } });

            var input = "[code]Here is some code[/code]\nMore text!";
            var expected = "<pre>Here is some code</pre>More text!"; // No newline after the closing PRE

            Assert.Equal(expected, parser.ToHtml(input));
        }
    }
}
