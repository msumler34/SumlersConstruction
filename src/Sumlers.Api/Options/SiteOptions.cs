namespace Sumlers.Api.Options;

/// <summary>
/// Business details shown across the site. appsettings.json holds the defaults; once the admin
/// page saves Business Info, App_Data/site.json replaces them.
/// </summary>
public sealed class SiteOptions
{
    public const string SectionName = "Site";

    public string BusinessName { get; set; } = "Sumlers Construction";
    public string Tagline { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string Address { get; set; } = "";
    public string ServiceArea { get; set; } = "";
    public string Hours { get; set; } = "";
    public string FacebookUrl { get; set; } = "";
    public string MessengerUrl { get; set; } = "";
    public int YearsInBusiness { get; set; }
    public string HeroImageUrl { get; set; } = "";
    public List<ServiceOffering> Services { get; set; } = [];
    public List<Testimonial> Testimonials { get; set; } = [];
    public List<GalleryImage> Gallery { get; set; } = [];

    // Page text. Nullable so a site.json saved before these existed still picks up the defaults.
    public HeroText? Hero { get; set; }
    public PageSection? WhyUs { get; set; }
    public PageSection? Process { get; set; }
    public SeoText? Seo { get; set; }

    /// <summary>Fills page-text sections that a saved site.json does not have yet.</summary>
    public SiteOptions WithMissingSectionsFrom(SiteOptions defaults)
    {
        Hero ??= defaults.Hero;
        WhyUs ??= defaults.WhyUs;
        Process ??= defaults.Process;
        Seo ??= defaults.Seo;
        return this;
    }
}

public sealed class HeroText
{
    public string Eyebrow { get; set; } = "";
    public string Title { get; set; } = "";
    public string Highlight { get; set; } = "";
    public string Text { get; set; } = "";
}

/// <summary>A home page section with a heading, intro text, optional photo and a list of points.</summary>
public sealed class PageSection
{
    public string Title { get; set; } = "";
    public string Text { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public List<SectionItem> Items { get; set; } = [];
}

public sealed class SectionItem
{
    public string Icon { get; set; } = "";
    public string Title { get; set; } = "";
    public string Text { get; set; } = "";
}

public sealed class SeoText
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
}

public sealed class ServiceOffering
{
    public string Key { get; set; } = "";
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Icon { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public List<string> Highlights { get; set; } = [];
}

public sealed class Testimonial
{
    public string Quote { get; set; } = "";
    public string Author { get; set; } = "";
    public string Project { get; set; } = "";
}

public sealed class GalleryImage
{
    public string ImageUrl { get; set; } = "";
    public string Label { get; set; } = "";
    public string Alt { get; set; } = "";
}
