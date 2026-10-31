using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MimeKit;
using Sumlers.Api.Email;

namespace Sumlers.Api.Tests;

/// <summary>Runs the API with a temp data folder and an in-memory email sender.</summary>
public sealed class SumlersApiFactory : WebApplicationFactory<Program>
{
    public const string AdminKey = "test-admin-key";

    private readonly string _DataDirectory = Path.Combine(Path.GetTempPath(), "sumlers-tests", Guid.NewGuid().ToString("N"));

    public List<MimeMessage> SentEmails { get; } = [];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Admin:ApiKey"] = AdminKey,
            ["Admin:DataDirectory"] = _DataDirectory,
        }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(new RecordingEmailSender(SentEmails));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (Directory.Exists(_DataDirectory)) Directory.Delete(_DataDirectory, recursive: true);
    }

    private sealed class RecordingEmailSender(List<MimeMessage> sent) : IEmailSender
    {
        public Task SendAsync(MimeMessage message, CancellationToken cancellationToken)
        {
            lock (sent) sent.Add(message);
            return Task.CompletedTask;
        }
    }
}
