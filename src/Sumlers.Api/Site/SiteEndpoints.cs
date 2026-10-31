using Sumlers.Api.Admin;
using Sumlers.Api.Backups;
using Sumlers.Api.Options;
using Sumlers.Api.Promotions;

namespace Sumlers.Api.Site;

public static class SiteEndpoints
{
    public static void MapSiteEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/site", async (ISiteStore store, CancellationToken ct) => Results.Ok(await store.GetAsync(ct)));
        app.MapGet("/api/promotions", GetActivePromotionsAsync);

        app.MapPut("/api/admin/site", SaveSiteAsync)
            .AddEndpointFilter<AdminKeyFilter>()
            .AddEndpointFilter<SnapshotBeforeChangeFilter>();
    }

    private static async Task<IResult> GetActivePromotionsAsync(IPromotionStore store, TimeProvider clock, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
        var promotions = await store.GetAllAsync(ct);
        return Results.Ok(promotions.Where(p => p.IsShownOn(today)));
    }

    private static async Task<IResult> SaveSiteAsync(SiteOptions site, ISiteStore store, CancellationToken ct)
    {
        var errors = SiteValidator.Validate(site);
        if (errors.Count > 0) return Results.ValidationProblem(errors);

        SiteValidator.AssignServiceKeys(site);
        await store.SaveAsync(site, ct);
        return Results.Ok(site);
    }
}
