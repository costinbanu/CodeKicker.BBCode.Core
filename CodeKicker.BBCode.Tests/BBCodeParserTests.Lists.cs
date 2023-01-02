using System.Collections.Generic;
using System.Web;
using Xunit;

namespace CodeKicker.BBCode.Core.Tests
{
    public partial class BBCodeParserTests
    {
        public class Lists : BBCodeParserTests
        {
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
                new BBTag("*", "<li>", "</li>", 20, autoRenderContent: true, BBTagClosingStyle.AutoCloseElement, x => x, enableIterationElementBehavior: true),
                new BBTag("list", "<${attr}>", "</${attr}>", 9, autoRenderContent: true, bbcodeUid: "", allowUrlProcessingAsText: true,
                    attributes : new[] { new BBAttribute("attr", "", a => string.IsNullOrWhiteSpace(a.AttributeValue) ? "ul" : $"ol type=\"{a.AttributeValue}\"") })
            };
                var parser = new BBCodeParser(bbcodes);
                Assert.Equal(expected, parser.ToHtml(input));
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
                    new BBTag("*", "<li>", "</li>", 20, autoRenderContent: true, BBTagClosingStyle.AutoCloseElement, contentTransformer: null, enableIterationElementBehavior: true),
                    new BBTag("list", "<${attr}>", "</${attr}>", 9, autoRenderContent: true, BBTagClosingStyle.RequiresClosingTag, contentTransformer: null, bbcodeUid : "", allowUrlProcessingAsText: true,
                        attributes: new[] {new BBAttribute("attr", "", a => string.IsNullOrWhiteSpace(a.AttributeValue) ? "ul" : $"ol type=\"{a.AttributeValue}\"") })
            });
                Assert.Equal(html, HttpUtility.HtmlDecode(parser.ToHtml(bbcode)));
            }

            [Fact]
            public void IgnoreNewlineAfterListItemTag()
            {
                Assert.Equal("<ul><li>item1</li><li>item2</li></ul>", TestUtils.BBEncodeForTest("[list]\r\n[*]item1\r\n[*]item2\r\n[/list]", ErrorMode.Strict, BBTagClosingStyle.AutoCloseElement, true));
            }
        }
    }
}
