using System.Text.Json;

namespace Sumlers.Api.Data;

/// <summary>Reads and writes the JSON files in App_Data that the admin page edits.</summary>
public static class JsonFile
{
    private static readonly JsonSerializerOptions _JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static async Task<T?> ReadAsync<T>(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path)) return default;
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, _JsonOptions, cancellationToken);
    }

    // Writes to a temp file first so a crash mid-write never leaves a half-written file.
    public static async Task WriteAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var tempPath = path + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, value, _JsonOptions, cancellationToken);
        }
        File.Move(tempPath, path, overwrite: true);
    }
}
