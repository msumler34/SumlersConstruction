using Sumlers.Api.Data;

namespace Sumlers.Api.Promotions;

public static class PromotionValidator
{
    public static ValidationErrors Validate(Promotion promotion) => new ValidationErrors()
        .AddIf(promotion.Title.Length > 120, nameof(Promotion.Title), "Title must be 120 characters or fewer.")
        .AddIf(string.IsNullOrWhiteSpace(promotion.Title), nameof(Promotion.Title), "Title is required.")
        .AddIf(promotion.Summary.Length > 600, nameof(Promotion.Summary), "Summary must be 600 characters or fewer.")
        .AddIf(!ValidationErrors.IsSafeUrl(promotion.ImageUrl), nameof(Promotion.ImageUrl), "Image URL must start with https://, http:// or /.")
        .AddIf(!ValidationErrors.IsSafeUrl(promotion.CtaUrl), nameof(Promotion.CtaUrl), "Button link must start with https://, http://, mailto:, tel: or /.")
        .AddIf(promotion.StartsOn > promotion.EndsOn, nameof(Promotion.EndsOn), "End date must be on or after the start date.");
}
