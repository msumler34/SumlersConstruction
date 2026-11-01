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
    public ServicesPageText? ServicesPage { get; set; }

    /// <summary>Fills page-text sections that a saved site.json does not have yet.</summary>
    public SiteOptions WithMissingSectionsFrom(SiteOptions defaults)
    {
        Hero ??= defaults.Hero;
        WhyUs ??= defaults.WhyUs;
        Process ??= defaults.Process;
        Seo ??= defaults.Seo;
        ServicesPage ??= defaults.ServicesPage;
        foreach (var service in Services) service.Page ??= new ServicePage();
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

    /// <summary>The main address, such as https://sumlerhomes.com. Google links and the sitemap use it.</summary>
    public string SiteUrl { get; set; } = "";
}

/// <summary>Text on the /services overview page.</summary>
public sealed class ServicesPageText
{
    public string Eyebrow { get; set; } = "";
    public string Title { get; set; } = "";
    public string Text { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public string MoreTitle { get; set; } = "";
    public string MoreText { get; set; } = "";
    public List<string> MoreItems { get; set; } = [];
    public string SeoTitle { get; set; } = "";
    public string SeoDescription { get; set; } = "";
}

public sealed class ServiceOffering
{
    /// <summary>The page address: the service page is at /{Key}.</summary>
    public string Key { get; set; } = "";
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Icon { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public List<string> Highlights { get; set; } = [];

    /// <summary>Earlier page addresses. They redirect to the current one, so old links and Google results keep working.</summary>
    public List<string> PreviousKeys { get; set; } = [];

    // Nullable so services saved before service pages existed still load.
    public ServicePage? Page { get; set; }
}

/// <summary>The content of a service's own page.</summary>
public sealed class ServicePage
{
    public string Heading { get; set; } = "";
    public string Intro { get; set; } = "";
    public List<TextBlock> Sections { get; set; } = [];
    public List<GalleryImage> Gallery { get; set; } = [];
    public List<Faq> Faqs { get; set; } = [];
    public string SeoTitle { get; set; } = "";
    public string SeoDescription { get; set; } = "";
}

/// <summary>A heading with text. Blank lines in the text start new paragraphs.</summary>
public sealed class TextBlock
{
    public string Title { get; set; } = "";
    public string Text { get; set; } = "";
}

public sealed class Faq
{
    public string Question { get; set; } = "";
    public string Answer { get; set; } = "";
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
