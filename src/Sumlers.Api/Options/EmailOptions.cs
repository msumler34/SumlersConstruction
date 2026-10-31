using System.Text.Json.Serialization;

namespace Sumlers.Api.Options;

/// <summary>
/// SMTP settings. appsettings.json holds the defaults; the admin Settings tab saves its own copy.
/// When Host is empty, messages are written as .eml files to App_Data/outbox instead of being sent.
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public bool UseStartTls { get; set; } = true;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromAddress { get; set; } = "";
    public string FromName { get; set; } = "Sumlers Construction Website";

    /// <summary>Who receives new estimate requests from the contact form.</summary>
    public List<string> NotifyAddresses { get; set; } = [];

    public bool SendCustomerConfirmation { get; set; } = true;

    [JsonIgnore]
    public bool UsesSmtp => !string.IsNullOrWhiteSpace(Host);
}

public sealed class AlertOptions
{
    public const string SectionName = "Alerts";

    /// <summary>Who gets an email when something goes wrong on the site.</summary>
    public List<string> Recipients { get; set; } = [];

    public bool EmailWeeklyBackup { get; set; }
}
