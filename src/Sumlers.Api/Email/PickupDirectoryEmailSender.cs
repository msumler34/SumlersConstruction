using Microsoft.Extensions.Options;
using MimeKit;
using Sumlers.Api.Options;

namespace Sumlers.Api.Email;

/// <summary>Writes each message as an .eml file. Used when no SMTP host is configured.</summary>
public sealed class PickupDirectoryEmailSender(
    IOptions<EmailOptions> options,
    IWebHostEnvironment environment,
    ILogger<PickupDirectoryEmailSender> logger) : IEmailSender
{
    private readonly string _Directory = Path.Combine(environment.ContentRootPath, options.Value.PickupDirectory);

    public async Task SendAsync(MimeMessage message, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_Directory);
        var path = Path.Combine(_Directory, $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.eml");
        await message.WriteToAsync(path, cancellationToken);
        logger.LogWarning("No SMTP host is configured. Saved email {Subject} to {Path}", message.Subject, path);
    }
}
