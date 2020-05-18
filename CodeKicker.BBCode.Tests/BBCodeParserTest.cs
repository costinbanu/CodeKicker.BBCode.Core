using CodeKicker.BBCode.Core.SyntaxTree;
using RandomTestValues;
using System.Linq;
using Xunit;

namespace CodeKicker.BBCode.Core.Tests
{
    public partial class BBCodeParserTest
    {
        [Fact]
        public void Test1()
        {
            Assert.Equal("", BBEncodeForTest("", ErrorMode.Strict));
        }

        [Fact]
        public void Test2()
        {
            Assert.Equal("a", BBEncodeForTest("a", ErrorMode.Strict));
            Assert.Equal(" a b c ", BBEncodeForTest(" a b c ", ErrorMode.Strict));
        }

        [Fact]
        public void Test3()
        {
            Assert.Equal("<b></b>", BBEncodeForTest("[b][/b]", ErrorMode.Strict));
        }

        [Fact]
        public void Test4()
        {
            Assert.Equal("text<b>text</b>text", BBEncodeForTest("text[b]text[/b]text", ErrorMode.Strict));
        }

        [Fact]
        public void Test5()
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

            try
            {
                BBEncodeForTest("[placeholder][/placeholder]", ErrorMode.Strict);
                
            }
            catch (BBCodeParsingException)
            {
            }

            try
            {
                BBEncodeForTest("[placeholder/]", ErrorMode.Strict);
                
            }
            catch (BBCodeParsingException)
            {
            }
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
            Assert.Equal("<li>1<b>a</b></li><li>2</li>", BBEncodeForTest("[*]1[b]a[*]2", ErrorMode.ErrorFree, BBTagClosingStyle.AutoCloseElement, true));

            try
            {
                BBEncodeForTest("[*]1[b]a[*]2", ErrorMode.Strict, BBTagClosingStyle.AutoCloseElement, true);
                
            }
            catch (BBCodeParsingException)
            {
            }
        }

        [Fact]
        public void TagContentTransformer()
        {
            var parser = new BBCodeParser(new[]
                {
                    new BBTag("b", "<b>", "</b>", true, true, content => content.Trim()), 
                });

            Assert.Equal("<b>abc</b>", parser.ToHtml("[b] abc [/b]"));
        }

        [Fact]
        public void AttributeValueTransformer()
        {
            var parser = new BBCodeParser(ErrorMode.Strict, null, new[]
                {
                    new BBTag("font", "<span style=\"${color}${font}\">", "</span>", true, true,
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
#pragma warning disable 168
            catch (BBCodeParsingException e)
#pragma warning restore 168
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
#pragma warning disable 168
            catch (BBCodeParsingException e)
#pragma warning restore 168
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
                    new BBTag("b", "<b>", "</b>"), 
                });
            var parserEmpty = new BBCodeParser(ErrorMode.Strict, "", new[]
                {
                    new BBTag("b", "<b>", "</b>"), 
                });
            var parserDiv = new BBCodeParser(ErrorMode.Strict, "<div>${content}</div>", new[]
                {
                    new BBTag("b", "<b>", "</b>"), 
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
            var parser = new BBCodeParser(ErrorMode.ErrorFree, null, new[] { new BBTag("code", "<pre>", "</pre>") { StopProcessing = true } });

            var input = "[code][i]This should [u]be a[/u] text literal[/i][/code]";
            var expected = "<pre>[i]This should [u]be a[/u] text literal[/i]</pre>";

            Assert.Equal(expected, parser.ToHtml(input));
        }

        [Fact]
        public void GreedyAttributeProcessing_ConsumesAllTokensForAttributeValue()
        {
            var parser = new BBCodeParser(ErrorMode.ErrorFree, null, new[] { new BBTag("quote", "<div><span>Posted by ${name}</span>", "</div>", new BBAttribute("name", "")) { GreedyAttributeProcessing = true } });

            var input = "[quote=Test User With Spaces]Here is my comment[/quote]";
            var expected = "<div><span>Posted by Test User With Spaces</span>Here is my comment</div>";

            Assert.Equal(expected, parser.ToHtml(input));
        }

        [Fact]
        public void NewlineTrailingOpeningTagIsIgnored()
        {
            var parser = new BBCodeParser(ErrorMode.ErrorFree, null, new[] { new BBTag("code", "<pre>", "</pre>") });

            var input = "[code]\nHere is some code[/code]";
            var expected = "<pre>Here is some code</pre>"; // No newline after the opening PRE

            Assert.Equal(expected, parser.ToHtml(input));
        }

        [Fact]
        public void SuppressFirstNewlineAfter_StopsFirstNewlineAfterClosingTag()
        {
            var parser = new BBCodeParser(ErrorMode.ErrorFree, null, new[] { new BBTag("code", "<pre>", "</pre>"){ SuppressFirstNewlineAfter = true } });

            var input = "[code]Here is some code[/code]\nMore text!";
            var expected = "<pre>Here is some code</pre>More text!"; // No newline after the closing PRE

            Assert.Equal(expected, parser.ToHtml(input));
        }
    }
}
