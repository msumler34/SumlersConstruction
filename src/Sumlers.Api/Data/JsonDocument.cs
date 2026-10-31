namespace Sumlers.Api.Data;

/// <summary>Something that caches data from App_Data. Restoring a backup reloads all of them.</summary>
public interface IReloadable
{
    void Reload();
}

/// <summary>
/// One JSON file in App_Data, cached in memory. Changes are written to disk before they become visible,
/// and a failed write drops the cache so memory never shows a change that was not saved.
/// </summary>
public sealed class JsonDocument<T>(string path, Func<CancellationToken, Task<T>> createDefault) : IReloadable
    where T : class
{
    private readonly SemaphoreSlim _Lock = new(1, 1);
    private T? _Cache;

    public async Task<T> GetAsync(CancellationToken cancellationToken)
    {
        await _Lock.WaitAsync(cancellationToken);
        try
        {
            return await LoadAsync(cancellationToken);
        }
        finally
        {
            _Lock.Release();
        }
    }

    /// <summary>Runs <paramref name="change"/> on the cached value and saves it when the change returns true.</summary>
    public async Task<bool> UpdateAsync(Func<T, bool> change, CancellationToken cancellationToken)
    {
        await _Lock.WaitAsync(cancellationToken);
        try
        {
            var value = await LoadAsync(cancellationToken);
            if (!change(value)) return false;
            await JsonFile.WriteAsync(path, value, cancellationToken);
            return true;
        }
        catch
        {
            _Cache = null;
            throw;
        }
        finally
        {
            _Lock.Release();
        }
    }

    public async Task SetAsync(T value, CancellationToken cancellationToken)
    {
        await _Lock.WaitAsync(cancellationToken);
        try
        {
            await JsonFile.WriteAsync(path, value, cancellationToken);
            _Cache = value;
        }
        finally
        {
            _Lock.Release();
        }
    }

    public void Reload() => _Cache = null;

    private async Task<T> LoadAsync(CancellationToken cancellationToken) =>
        _Cache ??= await JsonFile.ReadAsync<T>(path, cancellationToken) ?? await createDefault(cancellationToken);
}
