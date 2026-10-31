using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Sumlers.Api.Data;
using Sumlers.Api.Options;
using Sumlers.Api.Settings;

namespace Sumlers.Api.Email;

/// <summary>
/// Sends mail with the SMTP settings saved in the admin page, read at send time so changes apply at once.
/// Without an SMTP host it writes .eml files to App_Data/outbox, which keeps the contact form usable in development.
/// </summary>
public sealed class EmailSender(ISettingsStore settingsStore, AppDataFolder dataFolder, ILogger<EmailSender> logger) : IEmailSender
{
    public async Task SendAsync(MimeMessage message, CancellationToken cancellationToken)
    {
        var email = (await settingsStore.GetAsync(cancellationToken)).Email;
        if (message.From.Count == 0) message.From.Add(new MailboxAddress(email.FromName, email.FromAddress));

        if (email.UsesSmtp)
        {
            await SendSmtpAsync(message, email, cancellationToken);
        }
        else
        {
            await SaveToOutboxAsync(message, cancellationToken);
        }
    }

    private static async Task SendSmtpAsync(MimeMessage message, EmailOptions email, CancellationToken cancellationToken)
    {
        using var client = new SmtpClient { Timeout = 30_000 };
        var security = email.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
        await client.ConnectAsync(email.Host, email.Port, security, cancellationToken);
        if (!string.IsNullOrEmpty(email.Username))
        {
            await client.AuthenticateAsync(email.Username, email.Password, cancellationToken);
        }
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    private async Task SaveToOutboxAsync(MimeMessage message, CancellationToken cancellationToken)
    {
        var directory = dataFolder.PathFor("outbox");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.eml");
        await message.WriteToAsync(path, cancellationToken);
        logger.LogWarning("No SMTP host is configured. Saved email {Subject} to {Path}", message.Subject, path);
    }
}
