using System.Collections.Concurrent;
using MimeKit;
using Sumlers.Api.Email;
using Sumlers.Api.Settings;

namespace Sumlers.Api.Alerts;

public interface IAlertService
{
    /// <summary>
    /// Emails the alert recipients from the admin Settings tab. The same <paramref name="key"/> sends
    /// at most once an hour, so a repeating problem does not flood anyone's inbox.
    /// </summary>
    Task SendAsync(string key, string subject, string body, CancellationToken cancellationToken);
}

public sealed class AlertService(ISettingsStore settingsStore, IEmailSender sender, TimeProvider clock, ILogger<AlertService> logger)
    : IAlertService
{
    private static readonly TimeSpan _QuietPeriod = TimeSpan.FromHours(1);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _LastSent = new();

    public async Task SendAsync(string key, string subject, string body, CancellationToken cancellationToken)
    {
        logger.LogWarning("Alert {Key}: {Subject}. {Body}", key, subject, body);
        if (!TryClaim(key)) return;

        var recipients = (await settingsStore.GetAsync(cancellationToken)).Alerts.Recipients;
        if (recipients.Count == 0) return;
        try
        {
            await sender.SendAsync(Build(recipients, subject, body), cancellationToken);
        }
        catch (Exception ex)
        {
            // Alerts usually go out through the same mail server, so this often fails with it. The log still has it.
            logger.LogError(ex, "Could not email alert {Key}", key);
        }
    }

    private bool TryClaim(string key)
    {
        var now = clock.GetUtcNow();
        var claimed = false;
        _LastSent.AddOrUpdate(key, _ => { claimed = true; return now; }, (_, last) =>
        {
            claimed = now - last >= _QuietPeriod;
            return claimed ? now : last;
        });
        return claimed;
    }

    private static MimeMessage Build(List<string> recipients, string subject, string body)
    {
        var message = new MimeMessage { Subject = $"[Website alert] {subject}" };
        recipients.ForEach(r => message.To.Add(MailboxAddress.Parse(r)));
        message.Body = new TextPart("plain")
        {
            Text = $@"{body}

Sent by the Sumlers Construction website. You get this because your address is under Alerts in the admin Settings tab.",
        };
        return message;
    }
}
