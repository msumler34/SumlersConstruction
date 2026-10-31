using Sumlers.Api.Data;

namespace Sumlers.Api.Promotions;

/// <summary>
/// Keeps promotions in App_Data/promotions.json. The file is created from SeedData on first run,
/// and it can be edited by hand or from the admin page.
/// </summary>
public sealed class JsonPromotionStore : IPromotionStore, IReloadable
{
    private readonly JsonDocument<List<Promotion>> _Document;

    public JsonPromotionStore(AppDataFolder dataFolder, IWebHostEnvironment environment)
    {
        var seedPath = Path.Combine(environment.ContentRootPath, "SeedData", "promotions.json");
        _Document = new JsonDocument<List<Promotion>>(
            dataFolder.PathFor("promotions.json"),
            async ct => await JsonFile.ReadAsync<List<Promotion>>(seedPath, ct) ?? []);
    }

    public async Task<IReadOnlyList<Promotion>> GetAllAsync(CancellationToken cancellationToken)
    {
        var list = await _Document.GetAsync(cancellationToken);
        return [.. list.OrderBy(p => p.SortOrder).ThenBy(p => p.Title)];
    }

    public async Task<Promotion> SaveAsync(Promotion promotion, CancellationToken cancellationToken)
    {
        var saved = string.IsNullOrWhiteSpace(promotion.Id) ? promotion with { Id = Guid.NewGuid().ToString("N")[..12] } : promotion;
        await _Document.UpdateAsync(list =>
        {
            list.RemoveAll(p => p.Id == saved.Id);
            list.Add(saved);
            return true;
        }, cancellationToken);
        return saved;
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken) =>
        _Document.UpdateAsync(list => list.RemoveAll(p => p.Id == id) > 0, cancellationToken);

    public void Reload() => _Document.Reload();
}
