namespace Sumlers.Api.Promotions;

/// <summary>A topic promoted on the landing page, such as a seasonal special or a featured service.</summary>
public sealed record Promotion
{
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
    public string Summary { get; init; } = "";
    public string? Badge { get; init; }
    public string? ImageUrl { get; init; }
    public string? CtaText { get; init; }
    public string? CtaUrl { get; init; }

    /// <summary>The first active featured promotion shows as the large banner under the hero.</summary>
    public bool IsFeatured { get; init; }

    public bool IsActive { get; init; } = true;
    public int SortOrder { get; init; }
    public DateOnly? StartsOn { get; init; }
    public DateOnly? EndsOn { get; init; }

    public bool IsShownOn(DateOnly today) =>
        IsActive && (StartsOn is null || StartsOn <= today) && (EndsOn is null || EndsOn >= today);
}
