using Microsoft.AspNetCore.Diagnostics;

namespace Sumlers.Api.Alerts;

/// <summary>Emails the alert recipients about unexpected errors, then lets the default handler write the response.</summary>
public sealed class AlertingExceptionHandler(IAlertService alerts) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var request = $"{httpContext.Request.Method} {httpContext.Request.Path}";
        await alerts.SendAsync(
            $"error:{exception.GetType().Name}:{httpContext.Request.Path}",
            "The website hit an unexpected error",
            $@"Request: {request}
Error: {exception.GetType().Name}: {exception.Message}

Details:
{exception}",
            CancellationToken.None);
        return false;
    }
}
