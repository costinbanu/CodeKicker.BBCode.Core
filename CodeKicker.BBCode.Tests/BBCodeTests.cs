using CodeKicker.BBCode.Core.SyntaxTree;
using FsCheck.Xunit;
using RandomTestValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Xunit;
using Xunit.Abstractions;

namespace CodeKicker.BBCode.Core.Tests
{
    public partial class BBCodeTests
    {
        private readonly ITestOutputHelper _output;

        public BBCodeTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void DefaultParserWellconfigured()
        {
            try
            {
                BBCode.ToHtml(RandomValue.String());
            }
            catch (BBCodeParsingException)
            {
            }
        }

        [Fact]
        public void Escape_NoCrash()
        {
            BBCode.EscapeText(RandomValue.String());
        }

        [Theory, InlineData("fsdgsdf sdfghsgfdw34636 [] \" sdhdkfghdjkf ][ \" ssdds [[[ ]]]][][][[[]")]
        public void Escape_Unescape_Roundtrip(string text)
        {
            var escaped = BBCode.EscapeText(text);
            var unescaped = BBCode.UnescapeText(escaped);
            Assert.Equal(text, unescaped);
        }

        [Theory, InlineData("fsdgsdf sdfghsgfdw34636 [] \" sdhdkfghdjkf ][ \" ssdds [[[ ]]]][][][[[]")]
        public void EscapedStringIsSafeForParsing(string text)
        {
            var escaped = BBCode.EscapeText(text);

            var ast = new BBCodeParser(new List<BBTag>()).ParseSyntaxTree(escaped);

            if (text.Length == 0)
                Assert.Equal(0, ast.SubNodes.Count);
            else
                Assert.Equal(text, ((TextNode)ast.SubNodes.Single()).Text);
        }

        [Theory, InlineData("fsdgsdf sdfghsgfdw34636 [] \" sdhdkfghdjkf ][ \" ssdds [[[ ]]]][][][[[]")]
        public void Escape_Parse_ToText_Roundtrip(string text)
        {
            var escaped = BBCode.EscapeText(text);
            var unescaped = new BBCodeParser(new List<BBTag>()).ParseSyntaxTree(escaped);
            var text2 = unescaped.ToText();
            Assert.Equal(text, text2);
        }

        [Fact]
        public void ReplaceTextSpans_ManualTestCases()
        {
            ReplaceTextSpans_ManualTestCases_TestCase("", "", null, null);
            ReplaceTextSpans_ManualTestCases_TestCase("a", "a", null, null);
            ReplaceTextSpans_ManualTestCases_TestCase("[b]a[/b]", "[b]a[/b]", null, null);
            ReplaceTextSpans_ManualTestCases_TestCase("[b]a[/b]", "[b]a[/b]", txt => new[] { new TextSpanReplaceInfo(0, 0, null), }, null);
            ReplaceTextSpans_ManualTestCases_TestCase("[b]a[/b]", "[b][/b]", txt => new[] { new TextSpanReplaceInfo(0, 1, null), }, null);
            ReplaceTextSpans_ManualTestCases_TestCase("[b]a[/b]", "[b]x[/b]", txt => new[] { new TextSpanReplaceInfo(0, 1, new TextNode("x")), }, null);

            ReplaceTextSpans_ManualTestCases_TestCase("abc[b]def[/b]ghi[i]jkl[/i]", "xyabc[b]z[/b]g2i1[i]jkl[/i]",
                txt => txt switch
                {
                    "abc" => new[] { new TextSpanReplaceInfo(0, 0, new TextNode("x")), new TextSpanReplaceInfo(0, 0, new TextNode("y")), },
                    "def" => new[] { new TextSpanReplaceInfo(0, 3, new TextNode("z")), },
                    "ghi" => new[] { new TextSpanReplaceInfo(1, 1, new TextNode("2")), new TextSpanReplaceInfo(3, 0, new TextNode("1")), },
                    "jkl" => new[] { new TextSpanReplaceInfo(0, 0, new TextNode("w")), },
                    _ => null
                },
                tagNode => tagNode.Tag.Name != "i");

            static void ReplaceTextSpans_ManualTestCases_TestCase(string bbCode, string expected, Func<string, IList<TextSpanReplaceInfo>?>? getTextSpansToReplace, Func<TagNode, bool>? tagFilter)
            {
                var tree1 = TestUtils.GetParserForTest(ErrorMode.Strict, false, BBTagClosingStyle.AutoCloseElement, false).ParseSyntaxTree(bbCode);
                var tree2 = BBCode.ReplaceTextSpans(tree1, getTextSpansToReplace ?? (txt => Array.Empty<TextSpanReplaceInfo>()), tagFilter);
                Assert.Equal(expected, tree2!.ToBBCode());
            }
        }

        [Property]
        public void ReplaceTextSpans_WhenNoModifications_TreeIsPreserved(ErrorMode errorMode, BBTagClosingStyle bbTagClosingStyle)
        {
            var tree1 = TestUtils.GetAnyTree(errorMode, bbTagClosingStyle);
            var tree2 = BBCode.ReplaceTextSpans(tree1, txt => Array.Empty<TextSpanReplaceInfo>(), null);
            Assert.Equal(tree1, tree2);
        }

        [Property]
        public void ReplaceTextSpans_WhenEmptyModifications_TreeIsPreserved(ErrorMode errorMode, BBTagClosingStyle bbTagClosingStyle)
        {
            var tree1 = TestUtils.GetAnyTree(errorMode, bbTagClosingStyle);
            var tree2 = BBCode.ReplaceTextSpans(tree1, txt => new[] { new TextSpanReplaceInfo(0, 0, null), }, null);
            Assert.Equal(tree1.ToBBCode(), tree2?.ToBBCode());
        }

        [Property]
        public void ReplaceTextSpans_WhenEverythingIsConvertedToX_OutputContainsOnlyX_CheckedWithContains(ErrorMode errorMode, BBTagClosingStyle bbTagClosingStyle)
        {
            var tree1 = TestUtils.GetAnyTree(errorMode, bbTagClosingStyle);
            var tree2 = BBCode.ReplaceTextSpans(tree1, txt => new[] { new TextSpanReplaceInfo(0, txt.Length, new TextNode("x")), }, null);
            if (tree1.SubNodes.Count == 0)
            {
                Assert.Equal(0, tree2?.SubNodes.Count ?? 0);
            }
            else
            {
                Assert.Contains('x', tree2?.ToBBCode() ?? string.Empty);
            }
        }

        [Property]
        public void ReplaceTextSpans_WhenEverythingIsConvertedToX_OutputContainsOnlyX_CheckedWithTreeWalk(ErrorMode errorMode, BBTagClosingStyle bbTagClosingStyle)
        {
            var tree1 = TestUtils.GetAnyTree(errorMode, bbTagClosingStyle);
            var tree2 = BBCode.ReplaceTextSpans(tree1, txt => new[] { new TextSpanReplaceInfo(0, txt.Length, new TextNode("x")), }, null);
            var visitor = new TextAssertVisitor(str => Assert.True(str == "x"));
            visitor.Visit(tree2);
            Assert.Equal(tree1.SubNodes.Count > 0, visitor.AssertPerformed);
        }

        [Property]
        public void ReplaceTextSpans_ArbitraryTextSpans_NoCrash(ErrorMode errorMode, BBTagClosingStyle bbTagClosingStyle)
        {
            for (int i = 0; i < RandomValue.Int(100, 10); i++)
            {
                var tree1 = TestUtils.GetAnyTree(errorMode, bbTagClosingStyle);
                var chosenTexts = new List<string>();
                var tree2 = BBCode.ReplaceTextSpans(tree1, txt =>
                {
                    var count = RandomValue.Int(3, 0);
                    var indexes = new List<int>();
                    for (int i = 0; i < count; i++)
                    {
                        indexes.Add(RandomValue.Int(txt.Length, 0));
                    }
                    indexes.Sort();
                    _output.WriteLine(string.Join(", ", indexes));
                    return
                        Enumerable.Range(0, count)
                            .Select(i =>
                                {
                                    var maxIndex = i == count - 1 ? txt.Length : indexes[i + 1];
                                    var text = RandomValue.String();
                                    chosenTexts.Add(text);
                                    return new TextSpanReplaceInfo(indexes[i], RandomValue.Int(indexes[i] - maxIndex + 1, 0), new TextNode(text));
                                })
                            .ToArray();
                }, null);
                var bbCode = tree2?.ToBBCode();
                Assert.All(chosenTexts, s => Assert.Contains(s, bbCode));
            }
        }

        [Theory]
        [InlineData("8475h6jds", "[b:8475h6jds]this is some bold text[/b:8475h6jds]", "<b>this is some bold text</b>")]
        [InlineData("h75ks63nh5", "[url:h75ks63nh5]https://google.com[/url:h75ks63nh5]", "<a href=\"https://google.com\" target=\"_blank\">https://google.com</a>")]
        [InlineData("7845jh5674", "[url=https://google.com:7845jh5674]this is a custom link[/url:7845jh5674]", "<a href=\"https://google.com\" target=\"_blank\">this is a custom link</a>")]
        [InlineData("474h4gfs", "[quote=\"someone\":474h4gfs]some text[/quote:474h4gfs]", "<blockquote class=\"PostQuote\"><b>someone</b> wrote:<br/>some text</blockquote>")]
        public void BbcodeUid_IsHandled(string uid, string text, string expectedHtml)
        {
            Assert.Equal(expectedHtml, HttpUtility.HtmlDecode(TestUtils.GetCustomParser().ToHtml(text, uid)));
        }

        [Theory]
        [InlineData("8475h6jds", "[b:8475h6jds][i:8475h6jds]this is some bold italic text[/i:8475h6jds][/b:8475h6jds]", "<b><i>this is some bold italic text</i></b>")]
        [InlineData("7sh4g6b3j", "[b:7sh4g6b3j][url:7sh4g6b3j]https://google.com[/url:7sh4g6b3j][/b:7sh4g6b3j]", "<b><a href=\"https://google.com\" target=\"_blank\">https://google.com</a></b>")]
        [InlineData("87th8gfr", "[b:87th8gfr][url=https://google.com:87th8gfr]some text[/url:87th8gfr][/b:87th8gfr]", "<b><a href=\"https://google.com\" target=\"_blank\">some text</a></b>")]
        [InlineData("3q85xb4n", "[b:3q85xb4n][size=200:3q85xb4n][color=red:3q85xb4n]aaa[/color:3q85xb4n][/size:3q85xb4n][/b:3q85xb4n]", "<b><span style=\"font-size:2em\"><span style=\"color:red\">aaa</span></span></b>")]
        [InlineData("474h4gfs", "[quote=\"someone\":474h4gfs][quote=\"someone else\":474h4gfs]some nested text[/quote:474h4gfs][/quote:474h4gfs]", "<blockquote class=\"PostQuote\"><b>someone</b> wrote:<br/><blockquote class=\"PostQuote\"><b>someone else</b> wrote:<br/>some nested text</blockquote></blockquote>")]
        public void BbcodeUid_WhenNested_IsHandled(string uid, string text, string expectedHtml)
        {
            Assert.Equal(expectedHtml, HttpUtility.HtmlDecode(TestUtils.GetCustomParser().ToHtml(text, uid)));
        }

        [Theory]
        [InlineData(@"one\two\three", @"one\two\three")]
        [InlineData(@"one\\two\\three", @"one\two\three")]
        [InlineData(@"one\[two\]three", @"one[two]three")]
        public void EscapingChars_IsCorrect(string input, string expected)
        {
            Assert.Equal(expected, HttpUtility.HtmlDecode(TestUtils.GetCustomParser().ToHtml(input)));
        }


        class TextAssertVisitor : SyntaxTreeVisitor
        {
            readonly Action<string?> assertFunction;
            public bool AssertPerformed { get; private set; }

            public TextAssertVisitor(Action<string?> assertFunction)
            {
                this.assertFunction = assertFunction;
            }

            protected override SyntaxTreeNode? Visit(TextNode? node)
            {
                assertFunction(node?.Text);
                AssertPerformed = true;
                return node;
            }
        }
    }
}
