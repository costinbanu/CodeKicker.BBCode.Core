using CodeKicker.BBCode.Core.SyntaxTree;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace CodeKicker.BBCode.Core.Tests
{
    public partial class BBCodeParserTests
    {
        public class GenericParsing : BBCodeParserTests
        {
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

            [Fact]
            public void TagContentTransformer()
            {
                var parser = new BBCodeParser(new[]
                    {
                    new BBTag("b", "<b>", "</b>", 1, autoRenderContent: true, contentTransformer: content => content.Trim()),
                });

                Assert.Equal("<b>abc</b>", parser.ToHtml("[b] abc [/b]"));
            }

            [Theory]
            [InlineData("<span style=\"color:red;font-family:Arial;\">abc</span>", "[font color=red font=Arial]abc[/font]")]
            [InlineData("<span style=\"color:red;\">abc</span>", "[font color=red]abc[/font]")]
            public void AttributeValueTransformer(string expected, string actual)
            {
                var parser = new BBCodeParser(ErrorMode.Strict, null, new[]
                {
                    new BBTag("font", "<span style=\"${color}${font}\">", "</span>", 1, autoRenderContent: true, allowUrlProcessingAsText: true, attributes: new[] {
                        new BBAttribute("color", "color", attributeRenderingContext => string.IsNullOrEmpty(attributeRenderingContext.AttributeValue) ? "" : "color:" + attributeRenderingContext.AttributeValue + ";"),
                        new BBAttribute("font", "font", attributeRenderingContext => string.IsNullOrEmpty(attributeRenderingContext.AttributeValue) ? "" : "font-family:" + attributeRenderingContext.AttributeValue + ";") }),
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
            [InlineData(@"", true)]
            [InlineData(@"[b]abc[/b]", true)]
            [InlineData(@"[b]abc", false)]
            [InlineData(@"abc[0]def", false)]
            [InlineData(@"\", false)]
            [InlineData(@"\x", false)]
            [InlineData(@"[", false)]
            [InlineData(@"]", false)]
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
            [InlineData(@"\", @"\")]
            [InlineData(@"\x", @"\x")]
            [InlineData(@"\", @"\\")]
            [InlineData(@"\\", @"\\\\")]
            [InlineData(@"[", @"\[")]
            [InlineData(@"]", @"\]")]
            public void CorrectingErrorMode_Parsing(string expected, string actual)
            {
                Assert.Equal(expected, TestUtils.BBEncodeForTest(actual, ErrorMode.TryErrorCorrection));
            }

            [Theory]
            [InlineData(@"", @"")]
            [InlineData(@"abc", @"abc")]
            [InlineData(@"abc<b>def</b>", @"abc[b]def[/b]")]
            public void TextNodeHtml_NullTemplate(string expected, string actual)
            {
                var parserNull = new BBCodeParser(ErrorMode.Strict, null, new[]
                    {
                    new BBTag("b", "<b>", "</b>", 1),
                });
                Assert.Equal(expected, parserNull.ToHtml(actual));
            }

            [Theory]
            [InlineData(@"", @"")]
            [InlineData(@"", @"abc")]
            [InlineData(@"<b></b>", @"abc[b]def[/b]")]
            public void TextNodeHtml_EmptyTemplate(string expected, string actual)
            {
                var parserEmpty = new BBCodeParser(ErrorMode.Strict, "", new[]
                    {
                    new BBTag("b", "<b>", "</b>", 1),
                });

                Assert.Equal(expected, parserEmpty.ToHtml(actual));
            }

            [Theory]
            [InlineData(@"", @"")]
            [InlineData(@"<div>abc</div>", @"abc")]
            [InlineData(@"<div>abc</div><b><div>def</div></b>", @"abc[b]def[/b]")]
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
                var parser = new BBCodeParser(ErrorMode.ErrorFree, null, new[] { new BBTag("code", "<pre>", "</pre>", 1) });

                var input = "[code][i]This should [u]be a[/u] text literal[/i][/code]";
                var expected = "<pre>[i]This should [u]be a[/u] text literal[/i]</pre>";

                Assert.Equal(expected, parser.ToHtml(input));
            }

            [Fact]
            public void GreedyAttributeProcessing_ConsumesAllTokensForAttributeValue()
            {
                var parser = new BBCodeParser(ErrorMode.ErrorFree, null, new[] { new BBTag("quote", "<div><span>Posted by ${name}</span>", "</div>", 1, bbcodeUid: "", allowUrlProcessingAsText: true, greedyAttributeProcessing: true, attributes: new[] { new BBAttribute("name", "") }) });

                var input = "[quote=Test User With Spaces]Here is my comment[/quote]";
                var expected = "<div><span>Posted by Test User With Spaces</span>Here is my comment</div>";

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
        }
    }
}
