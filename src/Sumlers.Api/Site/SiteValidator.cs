using System.Net.Mail;
using System.Text.RegularExpressions;
using Sumlers.Api.Data;
using Sumlers.Api.Options;

namespace Sumlers.Api.Site;

public static partial class SiteValidator
{
    public static ValidationErrors Validate(SiteOptions site)
    {
        var errors = new ValidationErrors()
            .AddIf(string.IsNullOrWhiteSpace(site.BusinessName) || site.BusinessName.Length > 80, nameof(site.BusinessName), "Business name is required (80 characters max).")
            .AddIf(site.Tagline.Length > 200, nameof(site.Tagline), "Tagline must be 200 characters or fewer.")
            .AddIf(site.Phone.Length > 40, nameof(site.Phone), "Phone number is too long.")
            .AddIf(site.Email.Length > 0 && !MailAddress.TryCreate(site.Email, out _), nameof(site.Email), "Email address is not valid.")
            .AddIf(!ValidationErrors.IsHttpsUrl(site.FacebookUrl), nameof(site.FacebookUrl), "Facebook link must start with https://.")
            .AddIf(!ValidationErrors.IsHttpsUrl(site.MessengerUrl), nameof(site.MessengerUrl), "Messenger link must start with https:// (for example https://m.me/yourpage).")
            .AddIf(site.YearsInBusiness is < 0 or > 200, nameof(site.YearsInBusiness), "Years in business must be between 0 and 200.")
            .AddIf(!ValidationErrors.IsSafeUrl(site.HeroImageUrl), nameof(site.HeroImageUrl), "Hero image must start with https:// or /.")
            .AddIf(site.Services.Count > 12, nameof(site.Services), "Add at most 12 services.")
            .AddIf(site.Services.Any(s => string.IsNullOrWhiteSpace(s.Title)), nameof(site.Services), "Every service needs a title.")
            .AddIf(site.Services.Any(s => !ValidationErrors.IsSafeUrl(s.ImageUrl)), nameof(site.Services), "Service images must start with https:// or /.")
            .AddIf(site.Testimonials.Count > 30, nameof(site.Testimonials), "Add at most 30 reviews.")
            .AddIf(site.Testimonials.Any(t => string.IsNullOrWhiteSpace(t.Quote)), nameof(site.Testimonials), "Every review needs a quote.")
            .AddIf(site.Gallery.Count > 30, nameof(site.Gallery), "Add at most 30 gallery photos.")
            .AddIf(site.Gallery.Any(g => string.IsNullOrWhiteSpace(g.ImageUrl) || !ValidationErrors.IsSafeUrl(g.ImageUrl)), nameof(site.Gallery), "Every gallery photo needs an image starting with https:// or /.")
            .AddIf(site.Hero is { } hero && (hero.Title.Length > 80 || hero.Highlight.Length > 80 || hero.Text.Length > 400), nameof(site.Hero), "The headline is too long (80 characters per line, 400 for the text).")
            .AddIf(site.Seo is { } seo && (seo.Title.Length > 70 || seo.Description.Length > 300), nameof(site.Seo), "Keep the Google title under 70 characters and the description under 300.");
        AddSectionErrors(errors, site.WhyUs, nameof(site.WhyUs), "Why choose us");
        AddSectionErrors(errors, site.Process, nameof(site.Process), "How we work");
        return errors;
    }

    private static void AddSectionErrors(ValidationErrors errors, PageSection? section, string field, string name)
    {
        if (section is null) return;
        errors
            .AddIf(section.Items.Count > 12, field, $"{name}: add at most 12 points.")
            .AddIf(section.Items.Any(i => string.IsNullOrWhiteSpace(i.Title)), field, $"{name}: every point needs a title.")
            .AddIf(!ValidationErrors.IsSafeUrl(section.ImageUrl), field, $"{name}: the photo must start with https:// or /.");
    }

    /// <summary>Gives each service a unique URL-friendly key, used for links such as /services#decks.</summary>
    public static void AssignServiceKeys(SiteOptions site)
    {
        var used = new HashSet<string>();
        foreach (var service in site.Services)
        {
            var key = Slug(string.IsNullOrWhiteSpace(service.Key) ? service.Title : service.Key);
            var unique = key;
            for (var n = 2; !used.Add(unique); n++) unique = $"{key}-{n}";
            service.Key = unique;
        }
    }

    private static string Slug(string value)
    {
        var slug = NonAlphanumeric().Replace(value.ToLowerInvariant(), "-").Trim('-');
        return slug.Length == 0 ? "service" : slug;
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumeric();
}
