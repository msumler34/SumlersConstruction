using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Sumlers.Api.Admin;
using Sumlers.Api.Contact;
using Sumlers.Api.Email;
using Sumlers.Api.Options;
using Sumlers.Api.Promotions;
using Sumlers.Api.Site;

namespace Sumlers.Api;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSumlersServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SiteOptions>(configuration.GetSection(SiteOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.Configure<AdminOptions>(configuration.GetSection(AdminOptions.SectionName));
        services.Configure<ForwardedHeadersOptions>(o => o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPromotionStore, JsonPromotionStore>();
        services.AddSingleton<ISiteStore, JsonSiteStore>();
        services.AddSingleton<ContactEmailBuilder>();
        services.AddSingleton<AdminKeyFilter>();
        services.AddEmailSender(configuration);

        services.AddProblemDetails();
        services.AddRateLimiter(AddContactRateLimit);
        return services;
    }

    private static void AddEmailSender(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>() ?? new EmailOptions();
        if (options.UsesSmtp)
        {
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            services.AddSingleton<IEmailSender, PickupDirectoryEmailSender>();
        }
    }

    // Five messages per visitor every ten minutes is plenty for people and slows down spam bots.
    private static void AddContactRateLimit(Microsoft.AspNetCore.RateLimiting.RateLimiterOptions options)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy(ContactEndpoints.RateLimitPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(10) }));
    }
}
