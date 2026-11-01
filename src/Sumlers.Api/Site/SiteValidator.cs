using System.Net.Mail;
using System.Text.RegularExpressions;
using Sumlers.Api.Data;
using Sumlers.Api.Options;

namespace Sumlers.Api.Site;

public static partial class SiteValidator
{
    /// <summary>Addresses the site already uses, so a service page can never take one of them.</summary>
    public static readonly IReadOnlySet<string> ReservedKeys = new HashSet<string>
    {
        "services", "contact", "admin", "api", "health", "uploads", "media", "assets", "index", "sitemap", "robots", "favicon",
    };

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
            .AddIf(site.Services.Any(s => s.Key.Length > 60), nameof(site.Services), "Keep each service's web address under 60 characters.")
            .AddIf(site.Testimonials.Count > 30, nameof(site.Testimonials), "Add at most 30 reviews.")
            .AddIf(site.Testimonials.Any(t => string.IsNullOrWhiteSpace(t.Quote)), nameof(site.Testimonials), "Every review needs a quote.")
            .AddIf(site.Gallery.Count > 30, nameof(site.Gallery), "Add at most 30 gallery photos.")
            .AddIf(site.Gallery.Any(g => string.IsNullOrWhiteSpace(g.ImageUrl) || !ValidationErrors.IsSafeUrl(g.ImageUrl)), nameof(site.Gallery), "Every gallery photo needs an image starting with https:// or /.")
            .AddIf(site.Hero is { } hero && (hero.Title.Length > 80 || hero.Highlight.Length > 80 || hero.Text.Length > 400), nameof(site.Hero), "The headline is too long (80 characters per line, 400 for the text).")
            .AddIf(site.Seo is { } seo && (seo.Title.Length > 70 || seo.Description.Length > 300), nameof(site.Seo), "Keep the Google title under 70 characters and the description under 300.")
            .AddIf(site.Seo is { } address && !IsSiteAddress(address.SiteUrl), nameof(site.Seo), "The website address must look like https://sumlersconstruction.com, with nothing after the name.");
        AddSectionErrors(errors, site.WhyUs, nameof(site.WhyUs), "Why choose us");
        AddSectionErrors(errors, site.Process, nameof(site.Process), "How we work");
        AddServicesPageErrors(errors, site.ServicesPage);
        foreach (var service in site.Services) AddServicePageErrors(errors, service);
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

    private static void AddServicesPageErrors(ValidationErrors errors, ServicesPageText? page)
    {
        if (page is null) return;
        const string field = nameof(SiteOptions.ServicesPage);
        errors
            .AddIf(page.Title.Length > 120 || page.Text.Length > 600, field, "Services page: keep the headline under 120 characters and the text under 600.")
            .AddIf(!ValidationErrors.IsSafeUrl(page.ImageUrl), field, "Services page: the photo must start with https:// or /.")
            .AddIf(page.MoreItems.Count > 40, field, "Services page: add at most 40 other jobs.")
            .AddIf(page.SeoTitle.Length > 70 || page.SeoDescription.Length > 300, field, "Services page: keep the Google title under 70 characters and the description under 300.");
    }

    private static void AddServicePageErrors(ValidationErrors errors, ServiceOffering service)
    {
        if (service.Page is not { } page) return;
        var field = $"{nameof(SiteOptions.Services)}.{service.Title}";
        var name = string.IsNullOrWhiteSpace(service.Title) ? "A service" : service.Title;
        errors
            .AddIf(page.Heading.Length > 120 || page.Intro.Length > 600, field, $"{name}: keep the page headline under 120 characters and the intro under 600.")
            .AddIf(page.Sections.Count > 10, field, $"{name}: add at most 10 text sections.")
            .AddIf(page.Sections.Any(s => s.Title.Length > 120 || s.Text.Length > 4000), field, $"{name}: keep section headings under 120 characters and their text under 4000.")
            .AddIf(page.Gallery.Count > 16, field, $"{name}: add at most 16 photos.")
            .AddIf(page.Gallery.Any(g => string.IsNullOrWhiteSpace(g.ImageUrl) || !ValidationErrors.IsSafeUrl(g.ImageUrl)), field, $"{name}: every photo needs an image starting with https:// or /.")
            .AddIf(page.Faqs.Count > 20, field, $"{name}: add at most 20 questions.")
            .AddIf(page.Faqs.Any(f => string.IsNullOrWhiteSpace(f.Question) || string.IsNullOrWhiteSpace(f.Answer)), field, $"{name}: every question needs an answer.")
            .AddIf(page.SeoTitle.Length > 70 || page.SeoDescription.Length > 300, field, $"{name}: keep the Google title under 70 characters and the description under 300.");
    }

    private static bool IsSiteAddress(string url) =>
        string.IsNullOrWhiteSpace(url)
        || (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps && uri.AbsolutePath == "/" && uri.Query.Length == 0 && uri.Fragment.Length == 0);

    /// <summary>
    /// Gives each service a unique page address (/decks), never one the site already uses, and keeps
    /// each earlier address only while no current page has it.
    /// </summary>
    public static void AssignServiceKeys(SiteOptions site)
    {
        var used = new HashSet<string>(ReservedKeys);
        foreach (var service in site.Services)
        {
            var key = Slug(string.IsNullOrWhiteSpace(service.Key) ? service.Title : service.Key);
            var unique = key;
            for (var n = 2; !used.Add(unique); n++) unique = $"{key}-{n}";
            service.Key = unique;
        }

        foreach (var service in site.Services)
        {
            service.PreviousKeys = service.PreviousKeys.Select(Slug).Where(used.Add).TakeLast(10).ToList();
        }
    }

    public static string Slug(string value)
    {
        var slug = NonAlphanumeric().Replace(value.ToLowerInvariant(), "-").Trim('-');
        return slug.Length == 0 ? "service" : slug;
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumeric();
}
