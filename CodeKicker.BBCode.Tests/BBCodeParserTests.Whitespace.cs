using Xunit;

namespace CodeKicker.BBCode.Core.Tests
{
    public partial class BBCodeParserTests
    {
        public class Whitespace : BBCodeParserTests
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
                var parser = new BBCodeParser(ErrorMode.ErrorFree, null, new[] { new BBTag("code", "<pre>", "</pre>", 1, suppressFirstNewlineAfter: true) });

                var input = "[code]Here is some code[/code]\nMore text!";
                var expected = "<pre>Here is some code</pre>More text!"; // No newline after the closing PRE

                Assert.Equal(expected, parser.ToHtml(input));
            }
        }
    }
}
