namespace Sumlers.Api.Options;

public sealed class AdminOptions
{
    public const string SectionName = "Admin";

    /// <summary>Key the admin page sends in the X-Admin-Key header. Admin endpoints stay locked while it is empty.</summary>
    public string ApiKey { get; set; } = "";

    public string DataDirectory { get; set; } = "App_Data";
    public long MaxUploadBytes { get; set; } = 8 * 1024 * 1024;
}
