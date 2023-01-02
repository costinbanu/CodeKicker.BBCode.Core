using System.Web;
using Xunit;

namespace CodeKicker.BBCode.Core.Tests
{
    public partial class BBCodeParserTests
    {
        public class Urls : BBCodeParserTests
        {
            private readonly BBCodeParser _parser;

            public Urls() 
            {
                _parser = TestUtils.GetCustomParser();
            }

            [Fact]
            public void Url_IsCorrect()
            {
                Assert.Equal("<a href=\"http://example.org/path?name=value\">text</a>", TestUtils.BBEncodeForTest("[url=http://example.org/path?name=value]text[/url]", ErrorMode.Strict));
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
                Assert.Equal(expected, HttpUtility.HtmlDecode(_parser.ToHtml(input)));
            }

            [Theory]
            [InlineData("google.com", "google.com")]
            [InlineData("[url]google.com[/url]", "google.com")]
            [InlineData("[url=google.com]google[/url]", "google")]
            public void IncompleteUrls_AreNotParsed(string input, string expected)
            {
                Assert.Equal(expected, HttpUtility.HtmlDecode(_parser.ToHtml(input)));
            }
        }
    }
}