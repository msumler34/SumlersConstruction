using MimeKit;
using Sumlers.Api.Alerts;
using Sumlers.Api.Data;
using Sumlers.Api.Email;
using Sumlers.Api.Settings;

namespace Sumlers.Api.Backups;

/// <summary>
/// Runs once an hour: takes the daily backup, warns when the disk is nearly full, and emails the weekly
/// backup when that is turned on in Settings, so a copy lives somewhere other than this server.
/// </summary>
public sealed class BackupWorker(
    BackupService backups,
    ISettingsStore settingsStore,
    IEmailSender sender,
    IAlertService alerts,
    AppDataFolder dataFolder,
    TimeProvider clock,
    ILogger<BackupWorker> logger) : BackgroundService
{
    private const long LowDiskBytes = 500L * 1024 * 1024;
    private const long MaxAttachmentBytes = 15L * 1024 * 1024;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(1), clock, stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1), clock);
        do
        {
            await RunChecksAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunChecksAsync(CancellationToken ct)
    {
        await TryAsync("backup-failed", "The daily backup failed", () => backups.CreateIfOlderThanAsync(TimeSpan.FromHours(24), "daily", ct));
        await TryAsync("low-disk", "Could not check free disk space", () => CheckDiskSpaceAsync(ct));
        await TryAsync("weekly-backup-email", "The weekly backup email failed", () => EmailWeeklyBackupAsync(ct));
    }

    private async Task TryAsync(string key, string subject, Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "{Subject}", subject);
            await alerts.SendAsync(key, subject, ex.ToString(), CancellationToken.None);
        }
    }

    private async Task CheckDiskSpaceAsync(CancellationToken ct)
    {
        var free = new DriveInfo(Path.GetPathRoot(dataFolder.Root)!).AvailableFreeSpace;
        if (free >= LowDiskBytes) return;
        await alerts.SendAsync("low-disk", "The server is almost out of disk space",
            $"Only {free / 1024 / 1024} MB is free. New photos, messages and backups may fail to save.", ct);
    }

    private async Task EmailWeeklyBackupAsync(CancellationToken ct)
    {
        var alertSettings = (await settingsStore.GetAsync(ct)).Alerts;
        var marker = dataFolder.PathFor(Path.Combine("backups", "last-emailed.txt"));
        if (!alertSettings.EmailWeeklyBackup || alertSettings.Recipients.Count == 0) return;
        if (File.Exists(marker) && clock.GetUtcNow() - File.GetLastWriteTimeUtc(marker) < TimeSpan.FromDays(7)) return;

        var backup = await backups.CreateAsync("weekly", ct);
        await sender.SendAsync(BuildBackupEmail(alertSettings.Recipients, backup), ct);
        await File.WriteAllTextAsync(marker, clock.GetUtcNow().ToString("O"), ct);
    }

    private MimeMessage BuildBackupEmail(List<string> recipients, BackupInfo backup)
    {
        var message = new MimeMessage { Subject = $"Weekly website backup {backup.CreatedAt:yyyy-MM-dd}" };
        recipients.ForEach(r => message.To.Add(MailboxAddress.Parse(r)));
        var body = new BodyBuilder();
        if (backup.SizeBytes <= MaxAttachmentBytes)
        {
            body.TextBody = "The attached zip has all the website content, settings, messages and photos. Keep it: you can restore it from the admin page, Backups tab, Upload backup.";
            body.Attachments.Add(backups.PathOf(backup.Name)!);
        }
        else
        {
            body.TextBody = $"This week's backup is {backup.SizeBytes / 1024 / 1024} MB, too large to attach. Download {backup.Name} from the admin page, Backups tab, and keep a copy somewhere safe.";
        }
        message.Body = body.ToMessageBody();
        return message;
    }
}
