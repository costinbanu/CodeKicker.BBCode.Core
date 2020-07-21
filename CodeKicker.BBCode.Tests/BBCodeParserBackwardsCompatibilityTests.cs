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
     formatting[/b:{uid}]"
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
     formatting[/b:{uid}]"
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


[/b:{uid}]"
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
[/list:u:{uid}]"
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
[/list:u:{uid}]"
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
[/list:o:{uid}]"
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
    }
}
