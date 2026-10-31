using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using MimeKit;
using Sumlers.Api.Backups;
using Sumlers.Api.Email;

namespace Sumlers.Api.Tests;

/// <summary>Runs the API with a temp data folder, an in-memory email sender and no hourly backup job.</summary>
public sealed class SumlersApiFactory : WebApplicationFactory<Program>
{
    public const string AdminKey = "test-admin-key";

    private readonly string _DataDirectory = Path.Combine(Path.GetTempPath(), "sumlers-tests", Guid.NewGuid().ToString("N"));

    public List<MimeMessage> SentEmails { get; } = [];

    /// <summary>Makes every email fail, like a mail server that is down.</summary>
    public bool FailEmails { get; set; }

    public string DataDirectory => _DataDirectory;

    public HttpClient CreateAdminClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Admin-Key", AdminKey);
        return client;
    }

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
            services.AddSingleton<IEmailSender>(new RecordingEmailSender(this));
            var worker = services.Single(s => s.ImplementationType == typeof(BackupWorker));
            services.Remove(worker);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (Directory.Exists(_DataDirectory)) Directory.Delete(_DataDirectory, recursive: true);
    }

    private sealed class RecordingEmailSender(SumlersApiFactory factory) : IEmailSender
    {
        public Task SendAsync(MimeMessage message, CancellationToken cancellationToken)
        {
            if (factory.FailEmails) throw new InvalidOperationException("The mail server is down.");
            lock (factory.SentEmails) factory.SentEmails.Add(message);
            return Task.CompletedTask;
        }
    }
}
