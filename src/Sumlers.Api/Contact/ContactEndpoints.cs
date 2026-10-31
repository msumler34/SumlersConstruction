using Microsoft.Extensions.Options;
using MimeKit;
using Sumlers.Api.Email;
using Sumlers.Api.Options;
using Sumlers.Api.Site;

namespace Sumlers.Api.Contact;

public static class ContactEndpoints
{
    public const string RateLimitPolicy = "contact";

    public static void MapContactEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/contact", SendAsync).RequireRateLimiting(RateLimitPolicy);
    }

    private static async Task<IResult> SendAsync(
        ContactRequest request,
        ContactEmailBuilder builder,
        IEmailSender sender,
        IOptions<EmailOptions> options,
        ISiteStore siteStore,
        ILogger<ContactRequest> logger,
        CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Count > 0) return Results.ValidationProblem(errors);
        if (request.IsLikelySpam)
        {
            logger.LogInformation("Dropped a contact form submission that filled the honeypot field");
            return Results.Accepted();
        }

        await sender.SendAsync(builder.BuildLeadNotification(request), cancellationToken);
        logger.LogInformation("Sent estimate request from {Email} for {Service}", request.Email, request.Service);
        if (options.Value.SendCustomerConfirmation)
        {
            var site = await siteStore.GetAsync(cancellationToken);
            await TrySendConfirmationAsync(builder.BuildCustomerConfirmation(request, site), sender, logger, cancellationToken);
        }
        return Results.Accepted();
    }

    // The lead already reached the business, so a failed thank-you note must not fail the request.
    private static async Task TrySendConfirmationAsync(MimeMessage confirmation, IEmailSender sender, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            await sender.SendAsync(confirmation, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not send the confirmation email to {Recipients}", confirmation.To.ToString());
        }
    }
}
