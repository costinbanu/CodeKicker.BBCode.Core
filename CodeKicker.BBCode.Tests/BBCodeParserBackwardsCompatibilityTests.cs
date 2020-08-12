using System.Collections.Generic;
using System.Web;
using Xunit;

namespace CodeKicker.BBCode.Core.Tests
{
    public class BBCodeParserBackwardsCompatibilityTests
    {
        [Fact]
        public void Bitfield_WithUid_IsCorrect()
        {
            var parser = BBCodeTestUtil.GetCustomParser();
            var input = "[color=#0000FF:2xgytwj6]Lorem ipsum [url=https%3A%2F%2Fgoogle.com:2xgytwj6]dolor sit amet[/url:2xgytwj6][/color:2xgytwj6], consectetur adipiscing elit";
            var (_, _, bitfield) = parser.TransformForBackwardsCompatibility(input, "2xgytwj6");
            Assert.Equal("Eg==", bitfield);
        }

        [Fact]
        public void Bitfield_WithoutUid_IsCorrect()
        {
            var parser = BBCodeTestUtil.GetCustomParser();
            var input = "[color=#0000FF]Lorem ipsum [url=https%3A%2F%2Fgoogle.com]dolor sit amet[/url][/color], consectetur adipiscing elit";
            var (_, _, bitfield) = parser.TransformForBackwardsCompatibility(input);
            Assert.Equal("Eg==", bitfield);
        }

        [Fact]
        public void BackwardsCompatibility_WhenNoFormatting_IsCorrect()
        {
            var text = "some plain text";
            var parser = BBCodeTestUtil.GetCustomParser();
            Assert.Equal(text, parser.ToHtml(text));
        }

        [Fact]
        public void BackwardsCompatibility_WhenSimpleFormatting_IsCorrect()
        {
            var text = "[b]simple formatting[/b]";
            var parser = BBCodeTestUtil.GetCustomParser();
            var (bbCode, uid, _) = parser.TransformForBackwardsCompatibility(text);
            Assert.Equal($"[b:{uid}]simple formatting[/b:{uid}]", bbCode);
        }

        [Fact]
        public void BackwardsCompatibility_WhenSimpleFormatting_Whitespace1_IsCorrect()
        {
            var text =
@"[b]simple     
     formatting[/b]";
            var parser = BBCodeTestUtil.GetCustomParser();
            var (bbCode, uid, _) = parser.TransformForBackwardsCompatibility(text);
            Assert.Equal(
$@"[b:{uid}]simple     
     formatting[/b:{uid}]".Replace("\r", "")
                , bbCode);
        }

        [Fact]
        public void BackwardsCompatibility_WhenSimpleFormatting_Whitespace2_IsCorrect()
        {
            var text =
@"[b]
        simple     
     formatting[/b]";
            var parser = BBCodeTestUtil.GetCustomParser();
            var (bbCode, uid, _) = parser.TransformForBackwardsCompatibility(text);
            Assert.Equal(
$@"[b:{uid}]
        simple     
     formatting[/b:{uid}]".Replace("\r", "")
                , bbCode);
        }

        [Fact]
        public void BackwardsCompatibility_WhenSimpleFormatting_Whitespace3_IsCorrect()
        {
            var text =
@"[b]simple     
     formatting


[/b]";
            var parser = BBCodeTestUtil.GetCustomParser();
            var (bbCode, uid, _) = parser.TransformForBackwardsCompatibility(text);
            Assert.Equal(
$@"[b:{uid}]simple     
     formatting


[/b:{uid}]".Replace("\r", "")
                , bbCode);
        }

        [Fact]
        public void BackwardsCompatibility_WhenComplexFormatting_URL_IsCorrect()
        {
            var text = "[url=https://google.com]simple formatting[/url]";
            var parser = BBCodeTestUtil.GetCustomParser();
            var (bbCode, uid, _) = parser.TransformForBackwardsCompatibility(text);
            Assert.Equal($"[url=https://google.com:{uid}]simple formatting[/url:{uid}]", bbCode);
        }

        [Fact]
        public void BackwardsCompatibility_WhenComplexFormatting_UL_Test1_IsCorrect()
        {
            var text =
@"[list]
    [*]item1
    [*]item2
[/list]";
            var parser = BBCodeTestUtil.GetCustomParser();
            var (bbCode, uid, _) = parser.TransformForBackwardsCompatibility(text);
            Assert.Equal(
$@"[list:{uid}]
    [*:{uid}]item1[/*:m:{uid}]
    [*:{uid}]item2[/*:m:{uid}]
[/list:u:{uid}]".Replace("\r", "")
                , bbCode);
        }

        [Fact]
        public void BackwardsCompatibility_WhenComplexFormatting_UL_Test2_IsCorrect()
        {
            var text =
@"[url=https://google.com]some url right here[/url][list]
    [*][b]item1[/b]
    [*]item2
    [*]item3
    [*][b][i]item3[/i] is not[/b] over yet
    [*]item4
[/list]";
            var parser = BBCodeTestUtil.GetCustomParser();
            var (bbCode, uid, _) = parser.TransformForBackwardsCompatibility(text);
            Assert.Equal(
$@"[url=https://google.com:{uid}]some url right here[/url:{uid}][list:{uid}]
    [*:{uid}][b:{uid}]item1[/b:{uid}][/*:m:{uid}]
    [*:{uid}]item2[/*:m:{uid}]
    [*:{uid}]item3[/*:m:{uid}]
    [*:{uid}][b:{uid}][i:{uid}]item3[/i:{uid}] is not[/b:{uid}] over yet[/*:m:{uid}]
    [*:{uid}]item4[/*:m:{uid}]
[/list:u:{uid}]".Replace("\r", "")
                , bbCode);
        }

        [Fact]
        public void BackwardsCompatibility_WhenComplexFormatting_OL_IsCorrect()
        {
            var text =
@"[list=1]
    [*]item1
    [*]item2
[/list]";
            var parser = BBCodeTestUtil.GetCustomParser();
            var (bbCode, uid, _) = parser.TransformForBackwardsCompatibility(text);
            Assert.Equal(
$@"[list=1:{uid}]
    [*:{uid}]item1[/*:m:{uid}]
    [*:{uid}]item2[/*:m:{uid}]
[/list:o:{uid}]".Replace("\r", "")
                , bbCode);
        }

        [Fact]
        public void BackwardsCompatibility_WhenNestedFormatting_IsCorrect()
        {
            var text = "[b]simple [i]formatting[/i][/b]";
            var parser = BBCodeTestUtil.GetCustomParser();
            var (bbCode, uid, _) = parser.TransformForBackwardsCompatibility(text);
            Assert.Equal($"[b:{uid}]simple [i:{uid}]formatting[/i:{uid}][/b:{uid}]", bbCode);
        }

        [Fact]
        public void BackwardsCompatibility_WhenWrongFormatting_Nesting_IsCorrect()
        {
            var text = "[b]simple [i]formatting[/b][/i]";
            var parser = BBCodeTestUtil.GetCustomParser();
            var (bbCode, uid, _) = parser.TransformForBackwardsCompatibility(text);
            Assert.Equal(text, bbCode);
            Assert.NotNull(uid);
            Assert.Empty(uid);
        }

        [Theory]
        [InlineData("[b]simple [iformatting[/b][/i]")]
        [InlineData("[b]simple [x]formatting[/x][/i]")]
        [InlineData("[b]simple [x]formatting[/y][/i]")]
        [InlineData("[b]simple [b]formatting[/y][/i]")]
        public void BackwardsCompatibility_WhenWrongFormatting_Typo_IsCorrect(string text)
        {
            var parser = BBCodeTestUtil.GetCustomParser();
            var (bbCode, uid, _) = parser.TransformForBackwardsCompatibility(text);
            Assert.Equal(text, bbCode);
            Assert.NotNull(uid);
            Assert.Empty(uid);
        }

        [Fact]
        public void BackwardsCompatibility_InlineAttachment_IsCorrect()
        {
            var parser = BBCodeTestUtil.GetCustomParser();
            var (bbCode, uid, _) = parser.TransformForBackwardsCompatibility("[attachment=7]01.PNG[/attachment]");
            Assert.Equal($"[attachment=7:{uid}]<!-- ia7 -->01.PNG<!-- ia7 -->[/attachment:{uid}]", bbCode);
            Assert.NotNull(uid);
            Assert.NotEmpty(uid);
        }

        [Fact]
        public void BackwardsCompatibility_CarriageReturns_AreRemoved()
        {
            var parser = BBCodeTestUtil.GetCustomParser();
            var (bbCode, uid, _) = parser.TransformForBackwardsCompatibility("[url=\"https://google.com\"]\r\n[b]a link[/b]\r\n[/url]");
            Assert.Equal($"[url=\"https://google.com\":{uid}]\n[b:{uid}]a link[/b:{uid}]\n[/url:{uid}]", bbCode);
            Assert.NotNull(uid);
            Assert.NotEmpty(uid);
        }

        [Theory]
        [InlineData("<b>one</b>[b]two[/b]", "&lt;b&gt;one&lt;/b&gt;[b:dhrttn34]two[/b:dhrttn34]", "dhrttn34")]
        [InlineData("[b]<b>one</b>[/b]", "[b:dhrttn34]&lt;b&gt;one&lt;/b&gt;[/b:dhrttn34]", "dhrttn34")]
        [InlineData("<b>one</b>[attachment=7]01.PNG[/attachment]", "&lt;b&gt;one&lt;/b&gt;[attachment=7:dhrttn34]<!-- ia7 -->01.PNG<!-- ia7 -->[/attachment:dhrttn34]", "dhrttn34")]
        [InlineData("<b>a</b>\r\n[b]b[/b]\r\n[list]\r\n[*]1\r\n[list]\r\n[*]11\r\n[*]12\r\n[/list]\r\n[*]2\r\n[/list]\r\nx\r\n[attachment=0]y.jpg[/attachment]\r\nz",
            "&lt;b&gt;a&lt;/b&gt;\n[b:dhrttn34]b[/b:dhrttn34]\n[list:dhrttn34]\n[*:dhrttn34]1\n[list:dhrttn34]\n[*:dhrttn34]11[/*:m:dhrttn34]\n[*:dhrttn34]12[/*:m:dhrttn34]\n[/list:u:dhrttn34][/*:m:dhrttn34]\n[*:dhrttn34]2[/*:m:dhrttn34]\n[/list:u:dhrttn34]\nx\n[attachment=0:dhrttn34]<!-- ia0 -->y.jpg<!-- ia0 -->[/attachment:dhrttn34]\nz", 
            "dhrttn34")]
        [InlineData("[quote=\"user\"]", "[quote=&quot;user&quot;:7465hfgt]", "7465hfgt")]
        public void BackwardsCompatibility_EscapeHtml_IsCorrect(string input, string expected, string uid)
        {
            var parser = BBCodeTestUtil.GetCustomParser();
            var (bbCode, _, _) = parser.TransformForBackwardsCompatibility(input, uid);
            Assert.Equal(expected, bbCode);
        }

        [Theory]
        [InlineData("[list=1][*]one[list][*]bullet1[*]bullet2[/list][*]two[/list]", "<ol type=\"1\"><li>one<ul><li>bullet1</li><li>bullet2</li></ul></li><li>two</li></ol>")]
        [InlineData("[list][*]one[list][*]bullet1[*]bullet2[/list][*]two[/list]", "<ul><li>one<ul><li>bullet1</li><li>bullet2</li></ul></li><li>two</li></ul>")]
        [InlineData("[list=1][*]one[*][list][*]bullet1[*]bullet2[/list][*]two[/list]", "<ol type=\"1\"><li>one</li><li><ul><li>bullet1</li><li>bullet2</li></ul></li><li>two</li></ol>")]
        public void BackwardsCompatibility_BBCodeToHtml_SameAsRegular(string bbcode, string html)
        {
            var parser = new BBCodeParser(new List<BBTag>
            {
                    new BBTag("*", "<li>", "</li>", true, BBTagClosingStyle.AutoCloseElement, null, true, 20),
                    new BBTag("list", "<${attr}>", "</${attr}>", true, BBTagClosingStyle.RequiresClosingTag, null, 9, "",
                        new BBAttribute("attr", "", a => string.IsNullOrWhiteSpace(a.AttributeValue) ? "ul" : $"ol type=\"{a.AttributeValue}\""))
            });
            var (bbCode, uid, _) = parser.TransformForBackwardsCompatibility(bbcode);
            Assert.Equal(html, HttpUtility.HtmlDecode(parser.ToHtml(bbcode, uid)));
        }
    }
}
