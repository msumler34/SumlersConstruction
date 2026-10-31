using Sumlers.Api.Alerts;

namespace Sumlers.Api.Backups;

/// <summary>
/// Takes a backup before an admin change when the newest backup is over an hour old, so a bad edit
/// can always be undone. A failed backup sends an alert but does not block the change.
/// </summary>
public sealed class SnapshotBeforeChangeFilter(BackupService backups, IAlertService alerts) : IEndpointFilter
{
    private static readonly TimeSpan _MaxAge = TimeSpan.FromHours(1);

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            await backups.CreateIfOlderThanAsync(_MaxAge, "before-change", context.HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            await alerts.SendAsync("backup-failed", "A backup before an admin change failed", ex.ToString(), CancellationToken.None);
        }
        return await next(context);
    }
}
