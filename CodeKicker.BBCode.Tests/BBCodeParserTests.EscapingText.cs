using FsCheck;
using FsCheck.Xunit;
using System.Linq;
using Xunit;

namespace CodeKicker.BBCode.Core.Tests
{
    public partial class BBCodeParserTests
    {
        public class EscapingText : BBCodeParserTests
        {
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
        }
    }
}
