using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sumlers.Api.Backups;
using Sumlers.Api.Messages;
using Sumlers.Api.Options;
using Sumlers.Api.Settings;

namespace Sumlers.Api.Tests;

/// <summary>Settings, saved messages, backups and health. Each test gets its own site, so they cannot affect each other.</summary>
public sealed class OperationsTests : IDisposable
{
    private readonly SumlersApiFactory _Factory = new();

    private static readonly object _Contact = new
    {
        name = "Pat Homeowner",
        email = "pat@example.com",
        phone = "555-0100",
        service = "Custom Decks",
        address = "",
        message = "New deck please",
        website = "",
    };

    public void Dispose() => _Factory.Dispose();

    [Fact]
    public async Task Settings_PasswordIsNeverReturnedAndBlankKeepsIt()
    {
        var admin = _Factory.CreateAdminClient();
        var settings = Settings(password: "smtp-secret");
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync("/api/admin/settings", settings)).StatusCode);

        settings.Email.Password = "";
        await admin.PutAsJsonAsync("/api/admin/settings", settings);
        var view = await admin.GetFromJsonAsync<SettingsEndpoints.SettingsView>("/api/admin/settings");
        var publicSite = await _Factory.CreateClient().GetStringAsync("/api/site");
        var saved = await File.ReadAllTextAsync(Path.Combine(_Factory.DataDirectory, "settings.json"));

        Assert.True(view!.HasPassword);
        Assert.Equal("", view.Email.Password);
        Assert.DoesNotContain("smtp-secret", publicSite);
        Assert.Contains("smtp-secret", saved);
    }

    [Fact]
    public async Task Settings_RequireAdminKey()
    {
        var response = await _Factory.CreateClient().GetAsync("/api/admin/settings");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Contact_IsSavedWhenEmailFails_AndListedForAdmin()
    {
        _Factory.FailEmails = true;

        var response = await _Factory.CreateClient().PostAsJsonAsync("/api/contact", _Contact);
        var messages = await _Factory.CreateAdminClient().GetFromJsonAsync<List<ContactMessage>>("/api/admin/messages");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var message = Assert.Single(messages!);
        Assert.Equal("Pat Homeowner", message.Name);
        Assert.Equal(EmailStatus.Failed, message.EmailStatus);
    }

    [Fact]
    public async Task Contact_SentEmailIsMarkedSent()
    {
        await _Factory.CreateClient().PostAsJsonAsync("/api/contact", _Contact);
        var messages = await _Factory.CreateAdminClient().GetFromJsonAsync<List<ContactMessage>>("/api/admin/messages");

        Assert.Equal(EmailStatus.Sent, Assert.Single(messages!).EmailStatus);
    }

    [Fact]
    public async Task Backup_RestoreBringsBackOldContent_AndKeepsABackupOfTheCurrentState()
    {
        var admin = _Factory.CreateAdminClient();
        await SavePhoneAsync(admin, "(555) 111-1111");
        var backup = await (await admin.PostAsync("/api/admin/backups", null)).Content.ReadFromJsonAsync<BackupInfo>();
        await SavePhoneAsync(admin, "(555) 222-2222");

        var restore = await admin.PostAsync($"/api/admin/backups/{backup!.Name}/restore", null);
        var site = await _Factory.CreateClient().GetFromJsonAsync<SiteOptions>("/api/site");
        var backups = await admin.GetFromJsonAsync<List<BackupInfo>>("/api/admin/backups");

        Assert.Equal(HttpStatusCode.OK, restore.StatusCode);
        Assert.Equal("(555) 111-1111", site!.Phone);
        Assert.Contains(backups!, b => b.Reason == "before-restore");
    }

    [Fact]
    public async Task Backup_IsTakenBeforeTheFirstAdminChange()
    {
        var admin = _Factory.CreateAdminClient();

        await SavePhoneAsync(admin, "(555) 333-3333");
        var backups = await admin.GetFromJsonAsync<List<BackupInfo>>("/api/admin/backups");

        Assert.Contains(backups!, b => b.Reason == "before-change");
    }

    [Fact]
    public async Task Backup_UploadRejectsZipThatWritesOutsideItsFolder()
    {
        var zip = BuildZip(("backup.json", "{}"), ("uploads/../../evil.txt", "x"));

        var response = await _Factory.CreateAdminClient().PostAsync("/api/admin/backups/upload", new ByteArrayContent(zip));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Backup_UploadRejectsZipThatIsNotABackup()
    {
        var zip = BuildZip(("site.json", "{}"));

        var response = await _Factory.CreateAdminClient().PostAsync("/api/admin/backups/upload", new ByteArrayContent(zip));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("..%2Fsettings.json")]
    [InlineData("settings.json")]
    public async Task Backup_DownloadOnlyServesBackupFiles(string name)
    {
        var response = await _Factory.CreateAdminClient().GetAsync($"/api/admin/backups/{name}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Health_ReportsStorageAndEmailWithoutDetails()
    {
        var response = await _Factory.CreateClient().GetAsync("/health");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var checks = json.RootElement.GetProperty("checks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", checks.GetProperty("storage").GetProperty("status").GetString());
        Assert.Equal("Degraded", checks.GetProperty("email").GetProperty("status").GetString());
    }

    private static AdminSettings Settings(string password) => new()
    {
        Email = new EmailOptions
        {
            Host = "",
            Password = password,
            FromAddress = "site@example.com",
            NotifyAddresses = ["owner@example.com"],
        },
        Alerts = new AlertOptions { Recipients = ["alerts@example.com"] },
    };

    private async Task SavePhoneAsync(HttpClient admin, string phone)
    {
        var site = await _Factory.CreateClient().GetFromJsonAsync<SiteOptions>("/api/site");
        site!.Phone = phone;
        var response = await admin.PutAsJsonAsync("/api/admin/site", site);
        response.EnsureSuccessStatusCode();
    }

    private static byte[] BuildZip(params (string Name, string Content)[] entries)
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                using var writer = new StreamWriter(zip.CreateEntry(name).Open());
                writer.Write(content);
            }
        }
        return buffer.ToArray();
    }
}
