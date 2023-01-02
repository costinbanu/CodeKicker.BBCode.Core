using CodeKicker.BBCode.Core.SyntaxTree;
using FsCheck;
using FsCheck.Xunit;
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
            Assert.Equal(expected, TestUtils.BBEncodeForTest(input, ErrorMode.Strict));
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
                new BBTag("list", "<${attr}>", "</${attr}>", true, true, 9, "", true,
                    new BBAttribute("attr", "", a => string.IsNullOrWhiteSpace(a.AttributeValue) ? "ul" : $"ol type=\"{a.AttributeValue}\""))
            };
            var parser = new BBCodeParser(bbcodes);
            Assert.Equal(expected, parser.ToHtml(input));
        }

        [Fact]
        public void Url_IsCorrect()
        {
            Assert.Equal("<a href=\"http://example.org/path?name=value\">text</a>", TestUtils.BBEncodeForTest("[url=http://example.org/path?name=value]text[/url]", ErrorMode.Strict));
        }

        [Theory]
        [InlineData("xxxnameyyy", "[placeholder=name]")]
        [InlineData("xxxyyy", "[placeholder=]")]
        [InlineData("xxxyyy", "[placeholder]")]
        [InlineData("axxxyyyb", "a[placeholder]b")]
        [InlineData("<b>a</b>xxxyyy<b>b</b>", "[b]a[/b][placeholder][b]b[/b]")]
        public void LeafElementWithoutContent_IsCorrect(string expected, string actual)
        {
            Assert.Equal(expected, TestUtils.BBEncodeForTest(actual, ErrorMode.Strict));
        }

        [Theory]
        [InlineData("[placeholder][/placeholder]")]
        [InlineData("[placeholder/]")]
        public void LeafElementWithoutContent_WhenClosingTag_Throws(string actual)
        {
            Assert.Throws<BBCodeParsingException>(() => TestUtils.BBEncodeForTest(actual, ErrorMode.Strict));
        }

        [Fact]
        public void ImgTagHasNoContent()
        {
            Assert.Equal("<img src=\"url\" />", TestUtils.BBEncodeForTest("[img]url[/img]", ErrorMode.Strict));
        }

        [Theory]
        [InlineData("<li>item</li>", "[*]item")]
        [InlineData("<ul><li>item</li></ul>", "[list][*]item[/list]")]
        [InlineData("<li>item</li>", "[*]item[/*]")]
        [InlineData("<li><li>item</li></li>", "[*][*]item")]
        [InlineData("<li>1<li>2</li></li>", "[*]1[*]2")]
        public void ListItem_WhenAutoClose_IsCorrect(string expected, string actual)
        {
            Assert.Equal(expected, TestUtils.BBEncodeForTest(actual, ErrorMode.Strict, BBTagClosingStyle.AutoCloseElement, false));
        }

        [Theory]
        [InlineData("<li></li>item", "[*]item")]
        [InlineData("<ul><li></li>item</ul>", "[list][*]item[/list]")]
        [InlineData("<li></li><li></li>item", "[*][*]item")]
        [InlineData("<li></li>1<li></li>2", "[*]1[*]2")]
        public void ListItem_WhenLeaf_IsCorrect(string expected, string actual)
        {
            Assert.Equal(expected, TestUtils.BBEncodeForTest(actual, ErrorMode.Strict, BBTagClosingStyle.LeafElementWithoutContent, false));
        }

        [Theory]
        [InlineData("<li>item</li>", "[*]item")]
        [InlineData("<ul><li>item</li></ul>", "[list][*]item[/list]")]
        [InlineData("<li>item</li>", "[*]item[/*]")]
        [InlineData("<li></li><li>item</li>", "[*][*]item")]
        [InlineData("<li>1</li><li>2</li>", "[*]1[*]2")]
        [InlineData("<li>1<b>a</b></li><li>2</li>", "[*]1[b]a[/b][*]2")]
        public void ListItem_WhenAutoClose_And_IterationElement_IsCorrect(string expected, string actual)
        {
            Assert.Equal(expected, TestUtils.BBEncodeForTest(actual, ErrorMode.Strict, BBTagClosingStyle.AutoCloseElement, true));
        }

        [Fact]
        public void ListItem_WhenAutoClose_AndWrong_AndErrorFreeMode_ReturnsWrongHtml()
        {
            Assert.Equal("<li>1<b>a<li>2</li></b></li>", TestUtils.BBEncodeForTest("[*]1[b]a[*]2", ErrorMode.ErrorFree, BBTagClosingStyle.AutoCloseElement, true));
        }

        [Fact]
        public void ListItem_WhenAutoClose_AndWrong_AndErrorStringMode_Throws()
        {
            Assert.Throws<BBCodeParsingException>(() => TestUtils.BBEncodeForTest("[*]1[b]a[*]2", ErrorMode.Strict, BBTagClosingStyle.AutoCloseElement, true));
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
                    new BBTag("list", "<${attr}>", "</${attr}>", true, BBTagClosingStyle.RequiresClosingTag, null, 9, "", true,
                        new BBAttribute("attr", "", a => string.IsNullOrWhiteSpace(a.AttributeValue) ? "ul" : $"ol type=\"{a.AttributeValue}\""))
            });
            Assert.Equal(html, HttpUtility.HtmlDecode(parser.ToHtml(bbcode)));
        }

        [Fact]
        public void IgnoreNewlineAfterListItemTag()
        {
            Assert.Equal("<ul><li>item1</li><li>item2</li></ul>", TestUtils.BBEncodeForTest("[list]\r\n[*]item1\r\n[*]item2\r\n[/list]", ErrorMode.Strict, BBTagClosingStyle.AutoCloseElement, true));
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

        [Theory]
        [InlineData("<span style=\"color:red;font-family:Arial;\">abc</span>","[font color=red font=Arial]abc[/font]")]
        [InlineData("<span style=\"color:red;\">abc</span>","[font color=red]abc[/font]")]
        public void AttributeValueTransformer(string expected, string actual)
        {
            var parser = new BBCodeParser(ErrorMode.Strict, null, new[]
                {
                    new BBTag("font", "<span style=\"${color}${font}\">", "</span>", true, true, 1, "", true,
                        new BBAttribute("color", "color", attributeRenderingContext => string.IsNullOrEmpty(attributeRenderingContext.AttributeValue) ? "" : "color:" + attributeRenderingContext.AttributeValue + ";"),
                        new BBAttribute("font", "font", attributeRenderingContext => string.IsNullOrEmpty(attributeRenderingContext.AttributeValue) ? "" : "font-family:" + attributeRenderingContext.AttributeValue + ";")),
                });

            Assert.Equal(expected, parser.ToHtml(actual));
        }

        //the parser may never ever throw an exception other that BBCodeParsingException for any non-null input
        [Property]
        public void ErrorModeIsPredictable(ErrorMode errorMode, NonNull<string> input, BBTagClosingStyle listItemBbTagClosingStyle)
        {
            try
            {
                var output = TestUtils.BBEncodeForTest(input.Get, errorMode, listItemBbTagClosingStyle, false);
                Assert.DoesNotContain("<script", output);
            }
            catch (BBCodeParsingException)
            {
                Assert.NotEqual(ErrorMode.ErrorFree, errorMode);
            }
        }

        [Property]
        public void ErrorFreeModeAlwaysSucceeds(NonNull<string> randomText)
        {
            TestUtils.BBEncodeForTest(randomText.Get, ErrorMode.ErrorFree);
        }

        //no script-tags may be contained in the output under any circumstances
        [Property]
        public void NoScript_AnyInput(NonNull<string> input, ErrorMode errorMode)
        {
            try
            {
                var output = TestUtils.BBEncodeForTest(input.Get, errorMode);
                Assert.DoesNotContain("<script", output);
            }
            catch (BBCodeParsingException)
            {
                Assert.NotEqual(ErrorMode.ErrorFree, errorMode);
            }
        }

        //no script-tags may be contained in the output under any circumstances
        [Fact]
        public void NoScript_AnyInput_Tree()
        {
            var parser = TestUtils.GetParserForTest(ErrorMode.ErrorFree, true, BBTagClosingStyle.AutoCloseElement, false);
            var tree = TestUtils.CreateRootNode(parser.Tags.ToArray());
            var output = tree.ToHtml();
            Assert.True(!output.Contains("<script"));
        }

        //no html-chars may be contained in the output under any circumstances
        [Property(Arbitrary = new[] { typeof(Generators.NoNewLineString) })]
        public void NoHtmlChars_AnyInput(NonNull<string> input, ErrorMode errorMode)
        {
            try
            {
                var output = TestUtils.BBEncodeForTest(input.Get, errorMode);
                Assert.DoesNotContain('<', output);
                Assert.DoesNotContain('>', output);
            }
            catch (BBCodeParsingException)
            {
                Assert.NotEqual(ErrorMode.ErrorFree, errorMode);
            }
        }

        [Property]
        public void NoScript_FixedInput(ErrorMode errorMode)
        {
            Assert.DoesNotContain("<script", TestUtils.BBEncodeForTest("<script>", errorMode));
        }

        [Property]
        public void NoScriptInAttributeValue(ErrorMode errorMode)
        {
            Assert.DoesNotContain("<script", TestUtils.BBEncodeForTest("[url=<script>][/url]", errorMode));
        }

        //given a BBCode-string, parse it into a syntax tree, encode the tree in BBCode, parse it back into a second sytax tree and ensure that both are exactly equal
        [Property]
        public void RoundtripParsing_IsCorrect(NonNull<string> input, ErrorMode errorMode)
        {
            try
            {
                var parser = TestUtils.GetParserForTest(errorMode, false, BBTagClosingStyle.AutoCloseElement, false);
                var tree = parser.ParseSyntaxTree(input.Get);
                var bbcode = tree.ToBBCode();
                var tree2 = parser.ParseSyntaxTree(bbcode);
                Assert.Equal(tree, tree2);
            }
            catch (BBCodeParsingException)
            {
                Assert.NotEqual(ErrorMode.ErrorFree, errorMode);
            }
        }

        [Property]
        public void TextNodesCannotBeSplit(NonNull<string> input, ErrorMode errorMode)
        {
            var parser = TestUtils.GetParserForTest(errorMode, true, BBTagClosingStyle.AutoCloseElement, false);
            SequenceNode tree;
            try
            {
                tree = parser.ParseSyntaxTree(input.Get);
            }
            catch (BBCodeParsingException)
            {
                return;
            }

            AssertTextNodesNotSplit(tree);
        }

        static void AssertTextNodesNotSplit(SyntaxTreeNode node)
        {
            if (node.SubNodes is not null)
            {
                SyntaxTreeNode? lastNode = null;
                for (int i = 0; i < node.SubNodes.Count; i++)
                {
                    AssertTextNodesNotSplit(node.SubNodes[i]);
                    if (lastNode is not null)
                        Assert.False(lastNode is TextNode && node.SubNodes[i] is TextNode);
                    lastNode = node.SubNodes[i];
                }
            }
        }

        [Property]
        public void ToTextDoesNotCrash(NonNull<string> inputGenerator)
        {
            var input = inputGenerator.Get;
            var parser = TestUtils.GetParserForTest(ErrorMode.ErrorFree, true, BBTagClosingStyle.AutoCloseElement, false);
            var text = parser.ParseSyntaxTree(input).ToText();
            Assert.True(text.Length <= input.Length);
        }

        [Theory]
        [InlineData(@"",true)]
        [InlineData(@"[b]abc[/b]",true)]
        [InlineData(@"[b]abc",false)]
        [InlineData(@"abc[0]def",false)]
        [InlineData(@"\",false)]
        [InlineData(@"\x",false)]
        [InlineData(@"[",false)]
        [InlineData(@"]",false)]
        public void StrictErrorMode(string actual, bool isValid)
        {
            Assert.Equal(isValid, TestUtils.IsValid(actual, ErrorMode.Strict));
        }

        [Theory]
        [InlineData(@"")]
        [InlineData(@"[b]abc[/b]")]
        [InlineData(@"[b]abc")]
        public void CorrectingErrorMode_IsValid(string actual)
        {
            Assert.True(TestUtils.IsValid(actual, ErrorMode.TryErrorCorrection));
        }

        [Theory]
        [InlineData(@"\",@"\")]
        [InlineData(@"\x",@"\x")]
        [InlineData(@"\",@"\\")]
        [InlineData(@"\\",@"\\\\")]
        [InlineData(@"[",@"\[")]
        [InlineData(@"]",@"\]")]
        public void CorrectingErrorMode_Parsing(string expected, string actual)
        {
            Assert.Equal(expected, TestUtils.BBEncodeForTest(actual, ErrorMode.TryErrorCorrection));
        }

        [Theory]
        [InlineData(@"",@"")]
        [InlineData(@"abc",@"abc")]
        [InlineData(@"abc<b>def</b>",@"abc[b]def[/b]")]
        public void TextNodeHtml_NullTemplate(string expected, string actual)
        {
            var parserNull = new BBCodeParser(ErrorMode.Strict, null, new[]
                {
                    new BBTag("b", "<b>", "</b>", 1),
                });
            Assert.Equal(expected, parserNull.ToHtml(actual));
        }

        [Theory]
        [InlineData(@"",@"")]
        [InlineData(@"",@"abc")]
        [InlineData(@"<b></b>",@"abc[b]def[/b]")]
        public void TextNodeHtml_EmptyTemplate(string expected, string actual)
        {
            var parserEmpty = new BBCodeParser(ErrorMode.Strict, "", new[]
                {
                    new BBTag("b", "<b>", "</b>", 1),
                });

            Assert.Equal(expected, parserEmpty.ToHtml(actual));
        }

        [Theory]
        [InlineData(@"",@"")]
        [InlineData(@"<div>abc</div>",@"abc")]
        [InlineData(@"<div>abc</div><b><div>def</div></b>",@"abc[b]def[/b]")]
        public void TextNodeHtml_DivTemplate(string expected, string actual)
        {
            var parserDiv = new BBCodeParser(ErrorMode.Strict, "<div>${content}</div>", new[]
                {
                    new BBTag("b", "<b>", "</b>", 1),
                });

            Assert.Equal(expected, parserDiv.ToHtml(actual));
        }

        [Fact]
        public void ContentTransformer_EmptyAttribute_CanChooseValueFromAttributeRenderingContext()
        {
            var parser = TestUtils.GetParserForTest(ErrorMode.Strict, true, BBTagClosingStyle.AutoCloseElement, false);

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
            var parser = new BBCodeParser(ErrorMode.ErrorFree, null, new[] { new BBTag("quote", "<div><span>Posted by ${name}</span>", "</div>", 1, "", true, new BBAttribute("name", "")) { GreedyAttributeProcessing = true } });

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

        [Theory]
        [InlineData("[code][b]bold[/b][/code]", "<pre style=\"font-family: ui-monospace;\">[b]bold[/b]</pre>")]
        [InlineData("[code][b][i]bold italic[/i][/b][/code]", "<pre style=\"font-family: ui-monospace;\">[b][i]bold italic[/i][/b]</pre>")]
        [InlineData("[quote=\"someone\"][code][b][i]bold italic[/i][/b][/code][/quote]", "<blockquote class=\"PostQuote\"><b>someone</b> wrote:<br/><pre style=\"font-family: ui-monospace;\">[b][i]bold italic[/i][/b]</pre></blockquote>")]
        public void BBCodeInsideCodeTag_IsNotParsed(string input, string expected)
        {
            var parser = TestUtils.GetCustomParser();
            Assert.Equal(expected, parser.ToHtml(input));
        }

        [Theory]
        [InlineData("www.google.com", "<!-- m --><a href=\"//www.google.com\" target=\"_blank\">www.google.com</a><!-- m -->")]
        [InlineData("www.google.com/", "<!-- m --><a href=\"//www.google.com/\" target=\"_blank\">www.google.com/</a><!-- m -->")]
        [InlineData("<img src=\"emoji.jpg\"/>\nwww.google.com", "<img src=\"emoji.jpg\"/><br/><!-- m --><a href=\"//www.google.com\" target=\"_blank\">www.google.com</a><!-- m -->")]
        [InlineData("<!-- :) --><img src=\"emoji.jpg\"/><!-- :) -->\nwww.google.com", "<!-- :) --><img src=\"emoji.jpg\"/><!-- :) --><br/><!-- m --><a href=\"//www.google.com\" target=\"_blank\">www.google.com</a><!-- m -->")]
        [InlineData("www.google.com\nwww.google.com", "<!-- m --><a href=\"//www.google.com\" target=\"_blank\">www.google.com</a><!-- m --><br/><!-- m --><a href=\"//www.google.com\" target=\"_blank\">www.google.com</a><!-- m -->")]
        [InlineData("www.google.com\nwww.google.com\nblabla", "<!-- m --><a href=\"//www.google.com\" target=\"_blank\">www.google.com</a><!-- m --><br/><!-- m --><a href=\"//www.google.com\" target=\"_blank\">www.google.com</a><!-- m --><br/>blabla")]
        [InlineData("www.google.com\nhttps://nrk.no\nhttps://google.co.uk\nhttp://asomewhatbiggerlinkjustbecause.com",
            "<!-- m --><a href=\"//www.google.com\" target=\"_blank\">www.google.com</a><!-- m --><br/><!-- m --><a href=\"https://nrk.no\" target=\"_blank\">https://nrk.no</a><!-- m --><br/><!-- m --><a href=\"https://google.co.uk\" target=\"_blank\">https://google.co.uk</a><!-- m --><br/><!-- m --><a href=\"http://asomewhatbiggerlinkjustbecause.com\" target=\"_blank\">http://asomewhatbiggerlinkjustbecause.com</a><!-- m -->")]
        [InlineData("https://www.google.com/?q=something+to+search+for", "<!-- m --><a href=\"https://www.google.com/?q=something+to+search+for\" target=\"_blank\">https://www.google.com/?q=something+to+search+for</a><!-- m -->")]
        [InlineData("https://www.google.com/?q=something+longer+to+search+for", "<!-- m --><a href=\"https://www.google.com/?q=something+longer+to+search+for\" target=\"_blank\">https://www.google.com/?q=something+long ... arch+for</a><!-- m -->")]
        [InlineData("https://www.google.com/?q=ăîâșțåøæ", "<!-- m --><a href=\"https://www.google.com/?q=ăîâșțåøæ\" target=\"_blank\">https://www.google.com/?q=ăîâșțåøæ</a><!-- m -->")]
        [InlineData("https://www.google.com/maps/@59.8470853,10.810886,3a,75y,322.37h,90t/data=!3m7!1e1!3m5!1s-o5DL1mP1veq58zI0sm37w!2e0!6s%2F%2Fgeo1.ggpht.com%2Fcbk%3Fpanoid%3D-o5DL1mP1veq58zI0sm37w%26output%3Dthumbnail%26cb_client%3Dmaps_sv.tactile.gps%26thumb%3D2%26w%3D203%26h%3D100%26yaw%3D338.83408%26pitch%3D0%26thumbfov%3D100!7i16384!8i8192",
            "<!-- m --><a href=\"https://www.google.com/maps/@59.8470853,10.810886,3a,75y,322.37h,90t/data=!3m7!1e1!3m5!1s-o5DL1mP1veq58zI0sm37w!2e0!6s%2F%2Fgeo1.ggpht.com%2Fcbk%3Fpanoid%3D-o5DL1mP1veq58zI0sm37w%26output%3Dthumbnail%26cb_client%3Dmaps_sv.tactile.gps%26thumb%3D2%26w%3D203%26h%3D100%26yaw%3D338.83408%26pitch%3D0%26thumbfov%3D100!7i16384!8i8192\" target=\"_blank\">" +
                "https://www.google.com/maps/@59.8470853, ... 4!8i8192</a><!-- m -->")]
        [InlineData("bla www.google.com bla", "bla <!-- m --><a href=\"//www.google.com\" target=\"_blank\">www.google.com</a><!-- m --> bla")]
        [InlineData("bla(www.google.com)bla", "bla(<!-- m --><a href=\"//www.google.com\" target=\"_blank\">www.google.com</a><!-- m -->)bla")]
        [InlineData("https://www.google.com/?q=!@#$%&*-_=+|;:,.?", "<!-- m --><a href=\"https://www.google.com/?q\" target=\"_blank\">https://www.google.com/?q</a><!-- m -->=!@#$%&*-_=+|;:,.?")]
        [InlineData("https://www.google.com/?q=!@#$%&*-_=+|;:,.?'a", "<!-- m --><a href=\"https://www.google.com/?q\" target=\"_blank\">https://www.google.com/?q</a><!-- m -->=!@#$%&*-_=+|;:,.?'a")]
        [InlineData("bla www.google.com bla www.google.com", "bla <!-- m --><a href=\"//www.google.com\" target=\"_blank\">www.google.com</a><!-- m --> bla <!-- m --><a href=\"//www.google.com\" target=\"_blank\">www.google.com</a><!-- m -->")]
        [InlineData("[b]www.google.com[/b]", "<b><!-- m --><a href=\"//www.google.com\" target=\"_blank\">www.google.com</a><!-- m --></b>")]
        [InlineData("www.google.com\n\n[b]something[/b]", "<!-- m --><a href=\"//www.google.com\" target=\"_blank\">www.google.com</a><!-- m --><br/><br/><b>something</b>")]
        [InlineData("[b]1[/b] www.google.com [b]2[/b]", "<b>1</b> <!-- m --><a href=\"//www.google.com\" target=\"_blank\">www.google.com</a><!-- m --> <b>2</b>")]
        [InlineData("[b]1[/b]www.google.com[b]2[/b]", "<b>1</b><!-- m --><a href=\"//www.google.com\" target=\"_blank\">www.google.com</a><!-- m --><b>2</b>")]
        [InlineData("[b]some text[/b] [i]some more [attachment=0]file.jpg[/attachment] content [u]italic underline[/u][/i] www.google.com [b]some more text[/b]",
            "<b>some text</b> <i>some more #{AttachmentFileName=file.jpg/AttachmentIndex=0}# content <u>italic underline</u></i> <!-- m --><a href=\"//www.google.com\" target=\"_blank\">www.google.com</a><!-- m --> <b>some more text</b>")]
        [InlineData("[b]some text[/b] [i]some more [attachment=0]file.jpg[/attachment] content [u]italic underline[/u]www.google.com[/i] www.google.com [b]some more text[/b]",
            "<b>some text</b> <i>some more #{AttachmentFileName=file.jpg/AttachmentIndex=0}# content <u>italic underline</u><!-- m --><a href=\"//www.google.com\" target=\"_blank\">www.google.com</a><!-- m --></i> <!-- m --><a href=\"//www.google.com\" target=\"_blank\">www.google.com</a><!-- m --> <b>some more text</b>")]
        [InlineData("[url]www.google.com[/url]", "<a href=\"//www.google.com\" target=\"_blank\">www.google.com</a>")]
        [InlineData("[url]https://www.google.com?q=something&p=somethingElse[/url]", "<a href=\"https://www.google.com?q=something&p=somethingElse\" target=\"_blank\">https://www.google.com?q=something&p=somethingElse</a>")]
        [InlineData("[url=https://www.google.com?q=something&p=somethingElse]link[/url]", "<a href=\"https://www.google.com?q=something&p=somethingElse\" target=\"_blank\">link</a>")]
        [InlineData("https://www.google.com?q=something&p=somethingElse", "<!-- m --><a href=\"https://www.google.com?q=something&p=somethingElse\" target=\"_blank\">https://www.google.com?q=something&p=somethingElse</a><!-- m -->")]
        [InlineData("bla[url]www.google.com[/url]bla", "bla<a href=\"//www.google.com\" target=\"_blank\">www.google.com</a>bla")]
        [InlineData("[url=www.google.com]google[/url]", "<a href=\"//www.google.com\" target=\"_blank\">google</a>")]
        [InlineData("[img]https://www.google.com/images/branding/googlelogo/1x/googlelogo_color_272x92dp.png[/img]", "<br/><img src=\"https://www.google.com/images/branding/googlelogo/1x/googlelogo_color_272x92dp.png\" /><br/>")]
        [InlineData("<!-- m --><a href=\"https://www.google.com/?q=something+longer+to+search+for\" target=\"_blank\">https://www.google.com/?q=something+long ... arch+for</a><!-- m -->", "<!-- m --><a href=\"https://www.google.com/?q=something+longer+to+search+for\" target=\"_blank\">https://www.google.com/?q=something+long ... arch+for</a><!-- m -->")]
        [InlineData("<!-- s:) --><img src=\"www.some.url/icon_e_smile.gif\" alt=\":)\" title=\"Smile\" /><!-- s:) -->", "<!-- s:) --><img src=\"www.some.url/icon_e_smile.gif\" alt=\":)\" title=\"Smile\" /><!-- s:) -->")]
        [InlineData("<img src=\"www.some.url/i/dont/know/how/to/use/bbcode.jpg\" />", "<img src=\"www.some.url/i/dont/know/how/to/use/bbcode.jpg\" />")]
        [InlineData("[code]www.google.com[/code]", "<pre style=\"font-family: ui-monospace;\">www.google.com</pre>")]
        [InlineData("http aaa https aaaa www aaaaa", "http aaa https aaaa www aaaaa")]
        [InlineData("www.google.com.", "<!-- m --><a href=\"//www.google.com\" target=\"_blank\">www.google.com</a><!-- m -->.")]
        [InlineData("www.google.com... www.google.com", "<!-- m --><a href=\"//www.google.com\" target=\"_blank\">www.google.com</a><!-- m -->... <!-- m --><a href=\"//www.google.com\" target=\"_blank\">www.google.com</a><!-- m -->")]
        public void CreateUrlsFromText_IsCorrect(string input, string expected)
        {
            var parser = TestUtils.GetCustomParser();
            Assert.Equal(expected, HttpUtility.HtmlDecode(parser.ToHtml(input)));
        }

        [Theory]
        [InlineData("google.com", "google.com")]
        [InlineData("[url]google.com[/url]", "google.com")]
        [InlineData("[url=google.com]google[/url]", "google")]
        public void IncompleteUrls_AreNotParsed(string input, string expected)
        {
            var parser = TestUtils.GetCustomParser();
            Assert.Equal(expected, HttpUtility.HtmlDecode(parser.ToHtml(input)));
        }
    }
}
