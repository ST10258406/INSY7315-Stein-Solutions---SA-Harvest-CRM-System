namespace CRM.Application.Common.Utilities;

using System.Net;

/// <summary>
/// Builds the body fragment every outgoing email shares, so the donor email, the public-form
/// invite and the password reset all read the same inside the branded layout (EmailService wraps
/// the fragment in the header/footer). Inline styles only — Outlook and Gmail ignore most CSS.
/// Every piece of text passes through <see cref="WebUtility.HtmlEncode"/>, so nothing a user
/// typed (or a user's name) can reach the recipient as live markup.
/// </summary>
public static class EmailBodyHtml
{
    private const string Ink = "#17140F";
    private const string Yellow = "#FADF01";

    /// <summary>Plain text → one encoded &lt;p&gt; per line (no rich text).</summary>
    public static string Paragraphs(string plainText)
    {
        var lines = plainText.Replace("\r\n", "\n").Split('\n');
        return string.Concat(lines.Select(line => $"<p>{WebUtility.HtmlEncode(line)}</p>"));
    }

    /// <summary>
    /// A brand-yellow call-to-action button. Bulletproof-button pattern (a padded table cell
    /// around the link) so it renders in Outlook too.
    /// </summary>
    public static string Button(string url, string label) =>
        $"""
        <table role="presentation" cellpadding="0" cellspacing="0" style="margin:8px 0 24px;"><tr>
          <td style="background:{Yellow};border-radius:999px;">
            <a href="{WebUtility.HtmlEncode(url)}" style="display:inline-block;padding:14px 28px;font-size:15px;font-weight:700;color:{Ink};text-decoration:none;border-radius:999px;">{WebUtility.HtmlEncode(label)}</a>
          </td>
        </tr></table>
        """;
}
