using System.Net;
using System.Net.Http.Json;
using Sumlers.Api.Options;
using Sumlers.Api.Promotions;

namespace Sumlers.Api.Tests;

public sealed class ApiTests(SumlersApiFactory factory) : IClassFixture<SumlersApiFactory>
{
    private static readonly object _ValidContact = new
    {
        name = "Pat Homeowner",
        email = "pat@example.com",
        phone = "555-0100",
        service = "Custom Decks",
        address = "",
        message = "We want a new deck.\r\nBcc: someone@example.com",
        website = "",
    };

    [Fact]
    public async Task Site_ReturnsBusinessDetailsAndServices()
    {
        var site = await factory.CreateClient().GetFromJsonAsync<Dictionary<string, object>>("/api/site");

        Assert.Equal("Sumlers Construction", site!["businessName"].ToString());
        Assert.StartsWith("https://m.me/", site["messengerUrl"].ToString());
    }

    [Fact]
    public async Task Promotions_ReturnsOnlyActiveSeedTopicsInOrder()
    {
        var promotions = await factory.CreateClient().GetFromJsonAsync<List<Promotion>>("/api/promotions");

        Assert.NotEmpty(promotions!);
        Assert.All(promotions!, p => Assert.True(p.IsActive));
        Assert.Equal(promotions!.OrderBy(p => p.SortOrder).Select(p => p.Id), promotions!.Select(p => p.Id));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("wrong-key")]
    public async Task Admin_RejectsMissingOrWrongKey(string? key)
    {
        var client = factory.CreateClient();
        if (key is not null) client.DefaultRequestHeaders.Add("X-Admin-Key", key);

        var response = await client.GetAsync("/api/admin/promotions");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CreateHideAndDelete_ChangesWhatVisitorsSee()
    {
        var admin = AdminClient();
        var created = await (await admin.PostAsJsonAsync("/api/admin/promotions", new Promotion { Title = "Winter porch deal" }))
            .Content.ReadFromJsonAsync<Promotion>();
        Assert.Contains(await PublicTitlesAsync(), t => t == "Winter porch deal");

        await admin.PutAsJsonAsync($"/api/admin/promotions/{created!.Id}", created with { IsActive = false });
        Assert.DoesNotContain(await PublicTitlesAsync(), t => t == "Winter porch deal");

        var deleted = await admin.DeleteAsync($"/api/admin/promotions/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
    }

    [Fact]
    public async Task Admin_RejectsScriptLinks()
    {
        var response = await AdminClient().PostAsJsonAsync("/api/admin/promotions", new Promotion { Title = "Bad", CtaUrl = "javascript:alert(1)" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Contact_SendsLeadAndConfirmation()
    {
        factory.SentEmails.Clear();

        var response = await factory.CreateClient().PostAsJsonAsync("/api/contact", _ValidContact);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var lead = Assert.Single(factory.SentEmails, m => m.Subject?.StartsWith("New estimate request") == true);
        Assert.Equal("pat@example.com", lead.ReplyTo.Mailboxes.Single().Address);
        Assert.Contains(factory.SentEmails, m => m.To.Mailboxes.Any(a => a.Address == "pat@example.com"));
    }

    [Fact]
    public async Task Contact_HoneypotIsAcceptedButNotSent()
    {
        factory.SentEmails.Clear();
        var bot = new { name = "Bot", email = "bot@example.com", message = "Buy now", website = "http://spam.example" };

        var response = await factory.CreateClient().PostAsJsonAsync("/api/contact", bot);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Empty(factory.SentEmails);
    }

    [Fact]
    public async Task Contact_RejectsInvalidEmail()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/contact", new { name = "Pat", email = "not-an-email", message = "Hi" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SiteSave_RequiresAdminKey()
    {
        var response = await factory.CreateClient().PutAsJsonAsync("/api/admin/site", new SiteOptions());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SiteSave_RejectsNonHttpsMessengerLink()
    {
        var site = await factory.CreateClient().GetFromJsonAsync<SiteOptions>("/api/site");
        site!.MessengerUrl = "javascript:alert(1)";

        var response = await AdminClient().PutAsJsonAsync("/api/admin/site", site);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SiteSave_UpdatesPublicSiteAndUsesNewPhoneInConfirmation()
    {
        var site = await factory.CreateClient().GetFromJsonAsync<SiteOptions>("/api/site");
        site!.Phone = "(555) 867-5309";
        site.Address = "12 Oak Lane";
        site.Services.Add(new ServiceOffering { Title = "Deck Repair", Key = "decks" });

        var response = await AdminClient().PutAsJsonAsync("/api/admin/site", site);
        var updated = await factory.CreateClient().GetFromJsonAsync<SiteOptions>("/api/site");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("12 Oak Lane", updated!.Address);
        Assert.Equal(updated.Services.Count, updated.Services.Select(s => s.Key).Distinct().Count());
        Assert.Contains(updated.Services, s => s.Key == "decks-2");

        factory.SentEmails.Clear();
        await factory.CreateClient().PostAsJsonAsync("/api/contact", _ValidContact);
        Assert.Contains(factory.SentEmails, m => m.TextBody?.Contains("(555) 867-5309") == true);
    }

    private HttpClient AdminClient()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Admin-Key", SumlersApiFactory.AdminKey);
        return client;
    }

    private async Task<IEnumerable<string>> PublicTitlesAsync() =>
        (await factory.CreateClient().GetFromJsonAsync<List<Promotion>>("/api/promotions"))!.Select(p => p.Title);
}
