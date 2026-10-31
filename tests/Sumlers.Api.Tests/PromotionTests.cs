using Sumlers.Api.Promotions;

namespace Sumlers.Api.Tests;

public sealed class PromotionTests
{
    private static readonly DateOnly _Today = new(2026, 10, 31);

    [Theory]
    [InlineData(true, null, null, true)]
    [InlineData(false, null, null, false)]
    [InlineData(true, "2026-11-01", null, false)]
    [InlineData(true, "2026-10-31", "2026-10-31", true)]
    [InlineData(true, null, "2026-10-30", false)]
    public void IsShownOn_RespectsActiveFlagAndDateWindow(bool isActive, string? startsOn, string? endsOn, bool expected)
    {
        var promotion = new Promotion
        {
            Title = "Deck special",
            IsActive = isActive,
            StartsOn = startsOn is null ? null : DateOnly.Parse(startsOn),
            EndsOn = endsOn is null ? null : DateOnly.Parse(endsOn),
        };

        Assert.Equal(expected, promotion.IsShownOn(_Today));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,hi")]
    [InlineData("//evil.example.com")]
    public void Validate_RejectsUnsafeLinks(string url)
    {
        var errors = PromotionValidator.Validate(new Promotion { Title = "Deck", CtaUrl = url });

        Assert.Contains(nameof(Promotion.CtaUrl), errors.Keys);
    }

    [Theory]
    [InlineData("/contact?service=Decks")]
    [InlineData("https://www.facebook.com/sumlers")]
    [InlineData("tel:5555550123")]
    public void Validate_AcceptsSafeLinks(string url)
    {
        Assert.Empty(PromotionValidator.Validate(new Promotion { Title = "Deck", CtaUrl = url }));
    }

    [Fact]
    public void Validate_RequiresTitleAndOrderedDates()
    {
        var errors = PromotionValidator.Validate(new Promotion
        {
            Title = " ",
            StartsOn = new DateOnly(2026, 12, 1),
            EndsOn = new DateOnly(2026, 11, 1),
        });

        Assert.Contains(nameof(Promotion.Title), errors.Keys);
        Assert.Contains(nameof(Promotion.EndsOn), errors.Keys);
    }
}
