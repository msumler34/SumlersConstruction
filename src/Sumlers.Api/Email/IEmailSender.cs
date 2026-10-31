using MimeKit;

namespace Sumlers.Api.Email;

public interface IEmailSender
{
    Task SendAsync(MimeMessage message, CancellationToken cancellationToken);
}
