namespace CRM.Infrastructure.Services.Email;

using System.Net;
using CRM.Domain.Enums;

/// <summary>
/// Wraps an email body fragment in the branded header/footer. Table layout with inline styles
/// because Outlook and Gmail ignore most modern CSS. Staff-composed mail gets a "just reply" note
/// and a supporter footer; system mail gets a minimal "automated message" footer.
/// </summary>
internal static class EmailLayout
{
    private const string Ink = "#17140F";
    private const string Yellow = "#FADF01";

    private static bool IsPublicHttpsUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && !uri.IsLoopback
        && !uri.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase);

    public static string Wrap(
        string bodyHtml, string subject, bool staffComposed, BrevoSettings settings, string? replyToName, string? replyToEmail)
    {
        var org = WebUtility.HtmlEncode(settings.OrganisationName);

        // A localhost/non-HTTPS logo can't be fetched by the recipient's mail provider and would show as
        // a broken-image icon, so only emit the <img> for a publicly reachable HTTPS URL.
        var logoCell = !IsPublicHttpsUrl(settings.LogoUrl)
            ? ""
            : $"""<td width="64" valign="middle"><img src="{WebUtility.HtmlEncode(settings.LogoUrl)}" width="56" height="56" alt="{org}" style="display:block;border-radius:12px;border:0;"></td>""";

        const string tagline = "DONOR CRM";

        var replyNote = staffComposed && !string.IsNullOrWhiteSpace(replyToEmail)
            ? $"""
              <tr><td style="padding:0 40px 32px;">
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#FBF7E6;border:1px solid #F0E3A6;border-radius:8px;">
                  <tr><td style="padding:14px 18px;font-size:14px;line-height:1.5;color:#6B5A00;">
                    Just hit <strong>reply</strong> &mdash; your message goes straight to {WebUtility.HtmlEncode(replyToName ?? "us")} at <span style="color:{Ink};">{WebUtility.HtmlEncode(replyToEmail)}</span>.
                  </td></tr>
                </table>
              </td></tr>
              """
            : "";

        var details = string.IsNullOrWhiteSpace(settings.FooterDetails)
            ? ""
            : $"""<div>{WebUtility.HtmlEncode(settings.FooterDetails)}</div>""";

        var footer = staffComposed
            ? $"""
              <tr><td style="background:#F4F2EC;border-top:1px solid #E4DECE;padding:26px 40px;font-size:13.5px;line-height:1.6;color:#8A8374;">
                <div style="font-weight:700;color:#3F3A2E;margin-bottom:4px;">{org}</div>
                {details}
                <div style="margin-top:12px;padding-top:12px;border-top:1px solid #E4DECE;font-size:12.5px;color:#A69E8B;">Sent via SA Harvest CRM.</div>
              </td></tr>
              """
            : $"""
              <tr><td style="background:#F4F2EC;border-top:1px solid #E4DECE;padding:20px 40px;font-size:12.5px;line-height:1.6;color:#A69E8B;">
                {org} &middot; Automated message, please don&#39;t reply.
              </td></tr>
              """;

        return $$"""
            <!doctype html>
            <html lang="en">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>{{WebUtility.HtmlEncode(subject)}}</title>
            <style>p{margin:0 0 16px;} a{color:#8A5A00;}</style>
            </head>
            <body style="margin:0;padding:0;background:#F4F2EC;">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#F4F2EC;padding:24px 0;">
            <tr><td align="center">
              <table role="presentation" width="680" cellpadding="0" cellspacing="0" style="width:680px;max-width:100%;background:#FFFFFF;border:1px solid #E4DECE;border-radius:12px;overflow:hidden;font-family:'Helvetica Neue',Helvetica,Arial,sans-serif;color:{{Ink}};">
                <tr><td style="background:{{Ink}};padding:28px 36px;">
                  <table role="presentation" width="100%" cellpadding="0" cellspacing="0"><tr>
                    {{logoCell}}
                    <td valign="middle" style="padding-left:{{(logoCell.Length == 0 ? 0 : 14)}}px;">
                      <div style="font-size:22px;font-weight:800;color:#FFFFFF;">{{org}}</div>
                      <div style="font-size:12px;font-weight:700;letter-spacing:1.8px;color:{{Yellow}};margin-top:2px;">{{tagline}}</div>
                    </td>
                  </tr></table>
                </td></tr>
                <tr><td style="height:4px;line-height:4px;font-size:0;background:{{Yellow}};">&nbsp;</td></tr>
                <tr><td style="padding:36px 40px {{(staffComposed ? 30 : 36)}}px;font-size:16.5px;line-height:1.7;color:{{Ink}};">
                  {{bodyHtml}}
                </td></tr>
                {{replyNote}}
                {{footer}}
              </table>
            </td></tr>
            </table>
            </body>
            </html>
            """;
    }
}
