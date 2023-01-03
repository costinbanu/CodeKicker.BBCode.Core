using System.Web;
using Xunit;

namespace CodeKicker.BBCode.Core.Tests
{
    public partial class BBCodeParserTests
    {
        public class Attachments : BBCodeParserTests
        {
            private readonly BBCodeParser _parser;

            public Attachments() 
            {
                _parser = TestUtils.GetCustomParser();
            }

            [Theory]
            [InlineData("[attachment=0]somefile.jpg[/attachment]", "#{AttachmentFileName=somefile.jpg/AttachmentIndex=0}#")]
            [InlineData("[attachment=0]i am some random text![/attachment]", "#{AttachmentFileName=i am some random text!/AttachmentIndex=0}#")]
            [InlineData("[attachment=0][b]this should[/b]not be parsed[/attachment]", "#{AttachmentFileName=[b]this should[-b]not be parsed/AttachmentIndex=0}#")]
            [InlineData("[attachment=0]some/file/jpg[/attachment]", "#{AttachmentFileName=some-file-jpg/AttachmentIndex=0}#")]
            public void InlineAttachment_IsCorrect(string input, string expected)
            {
                Assert.Equal(expected, HttpUtility.HtmlDecode(_parser.ToHtml(input)));
            }
        }
    }
}
