using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Sumlers.Api.Admin;
using Sumlers.Api.Alerts;
using Sumlers.Api.Backups;
using Sumlers.Api.Contact;
using Sumlers.Api.Data;
using Sumlers.Api.Email;
using Sumlers.Api.Health;
using Sumlers.Api.Messages;
using Sumlers.Api.Options;
using Sumlers.Api.Promotions;
using Sumlers.Api.Settings;
using Sumlers.Api.Site;

namespace Sumlers.Api;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSumlersServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SiteOptions>(configuration.GetSection(SiteOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.Configure<AlertOptions>(configuration.GetSection(AlertOptions.SectionName));
        services.Configure<AdminOptions>(configuration.GetSection(AdminOptions.SectionName));
        services.Configure<ForwardedHeadersOptions>(o => o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<AppDataFolder>();
        services.AddStore<IPromotionStore, JsonPromotionStore>();
        services.AddStore<ISiteStore, JsonSiteStore>();
        services.AddStore<ISettingsStore, JsonSettingsStore>();
        services.AddStore<IMessageStore, JsonMessageStore>();

        services.AddSingleton<IEmailSender, EmailSender>();
        services.AddSingleton<IAlertService, AlertService>();
        services.AddSingleton<ContactEmailBuilder>();
        services.AddSingleton<AdminKeyFilter>();
        services.AddSingleton<BackupService>();
        services.AddSingleton<SnapshotBeforeChangeFilter>();
        services.AddHostedService<BackupWorker>();

        services.AddExceptionHandler<AlertingExceptionHandler>();
        services.AddProblemDetails();
        services.AddSumlersHealthChecks();
        services.AddRateLimiter(AddContactRateLimit);
        return services;
    }

    /// <summary>Registers a store under its interface and as <see cref="IReloadable"/>, so restoring a backup reloads it.</summary>
    private static void AddStore<TService, TStore>(this IServiceCollection services)
        where TService : class
        where TStore : class, TService, IReloadable
    {
        services.AddSingleton<TStore>();
        services.AddSingleton<TService>(sp => sp.GetRequiredService<TStore>());
        services.AddSingleton<IReloadable>(sp => sp.GetRequiredService<TStore>());
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
