using Microsoft.Extensions.Options;
using Sumlers.Api.Data;
using Sumlers.Api.Options;

namespace Sumlers.Api.Promotions;

/// <summary>
/// Keeps promotions in App_Data/promotions.json. The file is created from SeedData on first run,
/// and it can be edited by hand or from the admin page.
/// </summary>
public sealed class JsonPromotionStore(IOptions<AdminOptions> options, IWebHostEnvironment environment) : IPromotionStore
{
    private readonly SemaphoreSlim _Lock = new(1, 1);
    private readonly string _FilePath = Path.Combine(environment.ContentRootPath, options.Value.DataDirectory, "promotions.json");
    private readonly string _SeedPath = Path.Combine(environment.ContentRootPath, "SeedData", "promotions.json");
    private List<Promotion>? _Cache;

    public async Task<IReadOnlyList<Promotion>> GetAllAsync(CancellationToken cancellationToken)
    {
        await _Lock.WaitAsync(cancellationToken);
        try
        {
            return [.. (await LoadAsync(cancellationToken)).OrderBy(p => p.SortOrder).ThenBy(p => p.Title)];
        }
        finally
        {
            _Lock.Release();
        }
    }

    public async Task<Promotion> SaveAsync(Promotion promotion, CancellationToken cancellationToken)
    {
        var saved = string.IsNullOrWhiteSpace(promotion.Id) ? promotion with { Id = Guid.NewGuid().ToString("N")[..12] } : promotion;
        await MutateAsync(list =>
        {
            list.RemoveAll(p => p.Id == saved.Id);
            list.Add(saved);
            return true;
        }, cancellationToken);
        return saved;
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken) =>
        MutateAsync(list => list.RemoveAll(p => p.Id == id) > 0, cancellationToken);

    private async Task<bool> MutateAsync(Func<List<Promotion>, bool> change, CancellationToken cancellationToken)
    {
        await _Lock.WaitAsync(cancellationToken);
        try
        {
            var list = await LoadAsync(cancellationToken);
            if (!change(list)) return false;
            await JsonFile.WriteAsync(_FilePath, list, cancellationToken);
            return true;
        }
        catch
        {
            _Cache = null; // Reload from disk next time, so memory never shows a change that was not saved.
            throw;
        }
        finally
        {
            _Lock.Release();
        }
    }

    private async Task<List<Promotion>> LoadAsync(CancellationToken cancellationToken)
    {
        if (_Cache is not null) return _Cache;
        var source = File.Exists(_FilePath) ? _FilePath : _SeedPath;
        return _Cache = await JsonFile.ReadAsync<List<Promotion>>(source, cancellationToken) ?? [];
    }
}
