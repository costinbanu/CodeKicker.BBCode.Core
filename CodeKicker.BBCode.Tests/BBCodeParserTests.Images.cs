using System.Web;
using Xunit;

namespace CodeKicker.BBCode.Core.Tests
{
    public partial class BBCodeParserTests
    {
        public class Images : BBCodeParserTests
        {
            private readonly BBCodeParser _parser;

            public Images() 
            {
                _parser = TestUtils.GetCustomParser();
            }

            [Fact]
            public void ImgTagHasNoContent()
            {
                Assert.Equal("<br/><img src=\"url\" /><br/>", _parser.ToHtml("[img]url[/img]"));
                //Assert.Equal(, TestUtils.BBEncodeForTest(, ErrorMode.Strict));
            }

            [Fact]
            public void ImgTagHasNoRenderedChildren()
            {
                Assert.Equal("<br/><img src=\"[b]url[/b]\" /><br/>", _parser.ToHtml("[img][b]url[/b][/img]"));
                //Assert.Equal("<img src=\"[b]url[/b]\" />", TestUtils.BBEncodeForTest("[img][b]url[/b][/img]", ErrorMode.Strict));
            }
        }
    }
}
