using System.Net.Mail;
using MimeKit;
using Sumlers.Api.Admin;
using Sumlers.Api.Backups;
using Sumlers.Api.Data;
using Sumlers.Api.Email;
using Sumlers.Api.Options;

namespace Sumlers.Api.Settings;

public static class SettingsEndpoints
{
    /// <summary>What the admin page sees. The SMTP password never leaves the server; HasPassword says whether one is saved.</summary>
    public sealed record SettingsView(EmailOptions Email, AlertOptions Alerts, bool HasPassword);

    public static void MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var settings = app.MapGroup("/api/admin/settings").AddEndpointFilter<AdminKeyFilter>();
        settings.MapGet("/", async (ISettingsStore store, CancellationToken ct) => Results.Ok(ToView(await store.GetAsync(ct))));
        settings.MapPut("/", SaveAsync).AddEndpointFilter<SnapshotBeforeChangeFilter>();
        settings.MapPost("/test-email", SendTestEmailAsync);
    }

    private static async Task<IResult> SaveAsync(AdminSettings settings, ISettingsStore store, CancellationToken ct)
    {
        Normalize(settings);
        var errors = Validate(settings);
        if (errors.Count > 0) return Results.ValidationProblem(errors);

        // A blank password box means "keep the saved one".
        if (string.IsNullOrEmpty(settings.Email.Password))
        {
            settings.Email.Password = (await store.GetAsync(ct)).Email.Password;
        }
        await store.SaveAsync(settings, ct);
        return Results.Ok(ToView(settings));
    }

    private static async Task<IResult> SendTestEmailAsync(ISettingsStore store, IEmailSender sender, ILogger<AdminSettings> logger, CancellationToken ct)
    {
        var settings = await store.GetAsync(ct);
        var recipients = settings.Email.NotifyAddresses.Concat(settings.Alerts.Recipients).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (recipients.Count == 0) return Results.BadRequest(new { error = "Add at least one email address first." });

        var message = new MimeMessage { Subject = "Test email from the Sumlers Construction website" };
        recipients.ForEach(r => message.To.Add(MailboxAddress.Parse(r)));
        message.Body = new TextPart("plain") { Text = "If you are reading this, the website can send email. Nothing else to do." };
        try
        {
            await sender.SendAsync(message, ct);
            var where = settings.Email.UsesSmtp ? "Sent" : "No mail server is set, so it was saved to App_Data/outbox instead of sent";
            return Results.Ok(new { message = $"{where} to {string.Join(", ", recipients)}." });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Test email failed");
            return Results.BadRequest(new { error = $"The mail server refused the test email: {ex.Message}" });
        }
    }

    private static SettingsView ToView(AdminSettings settings)
    {
        var email = settings.Email;
        var copy = new EmailOptions
        {
            Host = email.Host,
            Port = email.Port,
            UseStartTls = email.UseStartTls,
            Username = email.Username,
            FromAddress = email.FromAddress,
            FromName = email.FromName,
            NotifyAddresses = [.. email.NotifyAddresses],
            SendCustomerConfirmation = email.SendCustomerConfirmation,
        };
        return new SettingsView(copy, settings.Alerts, HasPassword: email.Password.Length > 0);
    }

    private static void Normalize(AdminSettings settings)
    {
        settings.Email.Host = settings.Email.Host.Trim();
        settings.Email.NotifyAddresses = CleanAddresses(settings.Email.NotifyAddresses);
        settings.Alerts.Recipients = CleanAddresses(settings.Alerts.Recipients);
    }

    private static List<string> CleanAddresses(List<string> addresses) =>
        [.. addresses.Select(a => a.Trim()).Where(a => a.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase)];

    private static ValidationErrors Validate(AdminSettings settings)
    {
        var email = settings.Email;
        return new ValidationErrors()
            .AddIf(email.Port is < 1 or > 65535, "Port", "Port must be between 1 and 65535 (usually 587).")
            .AddIf(email.UsesSmtp && !IsEmail(email.FromAddress), "FromAddress", "Enter the address the website sends from.")
            .AddIf(email.UsesSmtp && email.NotifyAddresses.Count == 0, "NotifyAddresses", "Add at least one address for new estimate requests.")
            .AddIf(email.NotifyAddresses.Any(a => !IsEmail(a)), "NotifyAddresses", "One of the estimate request addresses is not valid.")
            .AddIf(settings.Alerts.Recipients.Any(a => !IsEmail(a)), "Recipients", "One of the alert addresses is not valid.")
            .AddIf(settings.Alerts.Recipients.Count > 10 || email.NotifyAddresses.Count > 10, "Recipients", "Add at most 10 addresses per list.");
    }

    private static bool IsEmail(string value) => MailAddress.TryCreate(value, out var parsed) && parsed.Address == value;
}
