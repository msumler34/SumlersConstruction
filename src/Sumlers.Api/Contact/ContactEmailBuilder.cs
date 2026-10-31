using System.Net;
using Microsoft.Extensions.Options;
using MimeKit;
using Sumlers.Api.Options;

namespace Sumlers.Api.Contact;

/// <summary>Builds the lead notification for the business and the thank-you note for the customer.</summary>
public sealed class ContactEmailBuilder(IOptions<EmailOptions> emailOptions)
{
    private readonly EmailOptions _Email = emailOptions.Value;

    public MimeMessage BuildLeadNotification(ContactRequest request)
    {
        var message = CreateMessage($"New estimate request from {OneLine(request.Name)}");
        message.To.Add(MailboxAddress.Parse(_Email.NotifyAddress));
        message.ReplyTo.Add(new MailboxAddress(OneLine(request.Name), request.Email.Trim()));
        message.Body = new BodyBuilder { TextBody = LeadText(request), HtmlBody = LeadHtml(request) }.ToMessageBody();
        return message;
    }

    public MimeMessage BuildCustomerConfirmation(ContactRequest request, SiteOptions site)
    {
        var message = CreateMessage($"Thanks for contacting {OneLine(site.BusinessName)}");
        message.To.Add(new MailboxAddress(OneLine(request.Name), request.Email.Trim()));
        if (!string.IsNullOrWhiteSpace(site.Email)) message.ReplyTo.Add(MailboxAddress.Parse(site.Email));
        message.Body = new BodyBuilder { TextBody = ConfirmationText(request, site), HtmlBody = ConfirmationHtml(request, site) }.ToMessageBody();
        return message;
    }

    private MimeMessage CreateMessage(string subject)
    {
        var message = new MimeMessage { Subject = subject };
        message.From.Add(new MailboxAddress(_Email.FromName, _Email.FromAddress));
        return message;
    }

    private static IEnumerable<(string Label, string Value)> Fields(ContactRequest request) =>
    [
        ("Name", request.Name), ("Email", request.Email), ("Phone", request.Phone ?? ""),
        ("Service", request.Service ?? ""), ("Project address", request.Address ?? ""),
    ];

    private static string LeadText(ContactRequest request) =>
        string.Join('\n', Fields(request).Select(f => $"{f.Label}: {f.Value}")) + $"\n\n{request.Message}";

    private static string LeadHtml(ContactRequest request)
    {
        var rows = string.Concat(Fields(request).Select(f =>
            $@"<tr><td style=""padding:6px 12px;color:#6b6257"">{f.Label}</td><td style=""padding:6px 12px""><strong>{Encode(f.Value)}</strong></td></tr>"));
        return $@"<div style=""font-family:Arial,sans-serif;color:#1f2a2e"">
<h2 style=""color:#b5651d"">New estimate request</h2>
<table style=""border-collapse:collapse"">{rows}</table>
<p style=""white-space:pre-wrap;border-left:4px solid #b5651d;padding:8px 12px;background:#f7f3ec"">{Encode(request.Message)}</p>
<p style=""color:#6b6257"">Reply to this email to answer the customer directly.</p></div>";
    }

    private static string ConfirmationText(ContactRequest request, SiteOptions site) =>
        $@"Hi {OneLine(request.Name)},

Thanks for reaching out to {site.BusinessName}. We received your message and will get back to you soon, usually within one business day.

{(site.Phone.Length > 0 ? $"If it is urgent, call us at {site.Phone}." : "")}

{site.BusinessName}";

    private static string ConfirmationHtml(ContactRequest request, SiteOptions site)
    {
        var urgent = site.Phone.Length > 0
            ? $@"<p>If it is urgent, call us at <a href=""tel:{Encode(site.Phone)}"">{Encode(site.Phone)}</a>.</p>"
            : "";
        return $@"<div style=""font-family:Arial,sans-serif;color:#1f2a2e;max-width:560px"">
<h2 style=""color:#b5651d"">Thanks, {Encode(OneLine(request.Name))}!</h2>
<p>We received your message and will get back to you soon, usually within one business day.</p>
{urgent}
<p style=""color:#6b6257"">{Encode(site.BusinessName)}</p></div>";
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    // Stops header injection through the name, which goes into the subject and the address display name.
    private static string OneLine(string value) => value.ReplaceLineEndings(" ").Trim();
}
