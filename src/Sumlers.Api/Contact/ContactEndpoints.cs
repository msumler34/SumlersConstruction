using MimeKit;
using Sumlers.Api.Alerts;
using Sumlers.Api.Email;
using Sumlers.Api.Messages;
using Sumlers.Api.Settings;
using Sumlers.Api.Site;

namespace Sumlers.Api.Contact;

public static class ContactEndpoints
{
    public const string RateLimitPolicy = "contact";

    public static void MapContactEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/contact", SendAsync).RequireRateLimiting(RateLimitPolicy);
    }

    /// <summary>Services the contact form needs, grouped so the endpoint signature stays readable.</summary>
    private sealed record ContactServices(
        ContactEmailBuilder Builder,
        IEmailSender Sender,
        IMessageStore Messages,
        ISettingsStore Settings,
        ISiteStore Site,
        IAlertService Alerts,
        TimeProvider Clock,
        ILogger<ContactRequest> Logger);

    private static async Task<IResult> SendAsync(ContactRequest request, [AsParameters] ContactServices services, CancellationToken ct)
    {
        var errors = request.Validate();
        if (errors.Count > 0) return Results.ValidationProblem(errors);
        if (request.IsLikelySpam)
        {
            services.Logger.LogInformation("Dropped a contact form submission that filled the honeypot field");
            return Results.Accepted();
        }

        // Saved before any email is tried, so a broken mail server never loses a lead.
        var saved = ContactMessage.From(request, services.Clock.GetUtcNow());
        await services.Messages.AddAsync(saved, ct);

        var settings = await services.Settings.GetAsync(ct);
        var status = await TrySendLeadAsync(request, saved, settings, services, ct);
        await services.Messages.UpdateAsync(saved.Id, m => m with { EmailStatus = status }, ct);

        if (settings.Email.SendCustomerConfirmation)
        {
            var site = await services.Site.GetAsync(ct);
            await TrySendConfirmationAsync(services.Builder.BuildCustomerConfirmation(request, site), services, ct);
        }
        return Results.Accepted();
    }

    private static async Task<EmailStatus> TrySendLeadAsync(
        ContactRequest request, ContactMessage saved, AdminSettings settings, ContactServices services, CancellationToken ct)
    {
        try
        {
            var recipients = settings.Email.NotifyAddresses;
            if (recipients.Count == 0) throw new InvalidOperationException("No address is set for new estimate requests.");
            await services.Sender.SendAsync(services.Builder.BuildLeadNotification(request, recipients), ct);
            services.Logger.LogInformation("Sent estimate request from {Email} for {Service}", request.Email, request.Service);
            return EmailStatus.Sent;
        }
        catch (Exception ex)
        {
            await services.Alerts.SendAsync("lead-email", "A new estimate request could not be emailed", $@"{saved.Name} ({saved.Email}, {saved.Phone}) sent an estimate request, but the email about it failed.
The request is saved: open the admin page and go to Messages.

Error: {ex.Message}", ct);
            return EmailStatus.Failed;
        }
    }

    // The lead is already saved, so a failed thank-you note must not fail the request.
    private static async Task TrySendConfirmationAsync(MimeMessage confirmation, ContactServices services, CancellationToken ct)
    {
        try
        {
            await services.Sender.SendAsync(confirmation, ct);
        }
        catch (Exception ex)
        {
            services.Logger.LogWarning(ex, "Could not send the confirmation email to {Recipients}", confirmation.To.ToString());
        }
    }
}
