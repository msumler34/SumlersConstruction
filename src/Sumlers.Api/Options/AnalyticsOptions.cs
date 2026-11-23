namespace Sumlers.Api.Options;

/// <summary>
/// Google tag (gtag.js) settings. Leave GoogleTagId empty and no Google script loads at all.
/// Set it in appsettings.json or as the Analytics__GoogleTagId environment variable on the server.
/// </summary>
public sealed class AnalyticsOptions
{
    public const string SectionName = "Analytics";

    /// <summary>The Google Ads tag ID (AW-123456789) or Google tag ID (G-… / GT-…). Empty turns the tag off.</summary>
    public string GoogleTagId { get; set; } = "";

    /// <summary>
    /// The conversion label of the Google Ads "phone call clicks" conversion (the part after the slash in
    /// send_to: 'AW-123456789/AbC-dEfGhIj'). Empty sends a plain click_to_call event instead.
    /// </summary>
    public string PhoneCallConversionLabel { get; set; } = "";
}
