using CRM.Application.Common.Utilities;

namespace CRM.Application.Tests.Common;

public class EmailBodyHtmlTests
{
    [Fact]
    public void Paragraphs_WrapsEachLineAndEncodesMarkup()
    {
        var html = EmailBodyHtml.Paragraphs("Hello\r\n<script>alert('x')</script>");

        Assert.Equal("<p>Hello</p><p>&lt;script&gt;alert(&#39;x&#39;)&lt;/script&gt;</p>", html);
    }

    [Fact]
    public void Button_RendersASingleLink_WithEncodedHrefAndLabel()
    {
        var html = EmailBodyHtml.Button("https://app.example.test/reset?token=a&email=b", "Reset <now>");

        Assert.Contains("href=\"https://app.example.test/reset?token=a&amp;email=b\"", html);
        Assert.Contains(">Reset &lt;now&gt;</a>", html);
        Assert.Single(html.Split("href=").Skip(1)); // the button only — no copy-the-link fallback
    }
}
