using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using Sumlers.Api.Data;

namespace Sumlers.Api.Backups;

public sealed record BackupInfo(string Name, DateTimeOffset CreatedAt, long SizeBytes, string Reason);

/// <summary>
/// Zips the site content in App_Data (content, settings, messages and uploaded photos) into App_Data/backups,
/// keeps the newest <see cref="KeepCount"/>, and restores any of them.
/// </summary>
public sealed partial class BackupService(AppDataFolder dataFolder, IEnumerable<IReloadable> stores, TimeProvider clock, ILogger<BackupService> logger)
{
    public const int KeepCount = 40;
    public const long MaxImportBytes = 1024L * 1024 * 1024;

    private const string ManifestName = "backup.json";
    private const string UploadsFolder = "uploads";
    private static readonly string[] _ContentFiles = ["site.json", "promotions.json", "settings.json", "messages.json"];

    private readonly SemaphoreSlim _Lock = new(1, 1);
    private string BackupDirectory => dataFolder.PathFor("backups");

    public IReadOnlyList<BackupInfo> List()
    {
        if (!Directory.Exists(BackupDirectory)) return [];
        return [.. Directory.EnumerateFiles(BackupDirectory, "*.zip")
            .Select(path => new FileInfo(path))
            .Where(f => BackupName().IsMatch(f.Name))
            .Select(ToInfo)
            .OrderByDescending(b => b.Name, StringComparer.Ordinal)];
    }

    /// <summary>The full path of a backup, or null when the name is not one of ours. Blocks path tricks like "..\".</summary>
    public string? PathOf(string name)
    {
        if (!BackupName().IsMatch(name)) return null;
        var path = Path.Combine(BackupDirectory, name);
        return File.Exists(path) ? path : null;
    }

    public async Task<BackupInfo> CreateAsync(string reason, CancellationToken cancellationToken)
    {
        await _Lock.WaitAsync(cancellationToken);
        try
        {
            return CreateUnlocked(reason);
        }
        finally
        {
            _Lock.Release();
        }
    }

    /// <summary>Creates a backup only when the newest one (of any kind) is older than <paramref name="maxAge"/>.</summary>
    public async Task<BackupInfo?> CreateIfOlderThanAsync(TimeSpan maxAge, string reason, CancellationToken cancellationToken)
    {
        var newest = List().FirstOrDefault();
        if (newest is not null && clock.GetUtcNow() - newest.CreatedAt < maxAge) return null;
        return await CreateAsync(reason, cancellationToken);
    }

    /// <summary>Replaces the current content with a backup. A "before-restore" backup is taken first, so this can be undone.</summary>
    public async Task RestoreAsync(string name, CancellationToken cancellationToken)
    {
        var path = PathOf(name) ?? throw new FileNotFoundException("Backup not found.", name);
        await _Lock.WaitAsync(cancellationToken);
        try
        {
            using (var zip = ZipFile.OpenRead(path))
            {
                EnsureValid(zip);
            }
            // No pruning yet: restoring the oldest backup must not delete it halfway through.
            CreateUnlocked("before-restore", prune: false);
            ReplaceContentWith(path);
            Prune();
            foreach (var store in stores) store.Reload();
            logger.LogWarning("Restored backup {Name}", name);
        }
        finally
        {
            _Lock.Release();
        }
    }

    /// <summary>Saves an uploaded backup zip into the backup list after checking it really is one.</summary>
    public async Task<BackupInfo> ImportAsync(Stream upload, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(BackupDirectory);
        var path = Path.Combine(BackupDirectory, NewName("uploaded"));
        var tempPath = path + ".tmp";
        try
        {
            await using (var file = File.Create(tempPath))
            {
                await upload.CopyToAsync(file, cancellationToken);
            }
            using (var zip = ZipFile.OpenRead(tempPath))
            {
                EnsureValid(zip);
            }
            File.Move(tempPath, path);
            return ToInfo(new FileInfo(path));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    private BackupInfo CreateUnlocked(string reason, bool prune = true)
    {
        Directory.CreateDirectory(BackupDirectory);
        var path = Path.Combine(BackupDirectory, NewName(reason));
        var tempPath = path + ".tmp";
        using (var zip = ZipFile.Open(tempPath, ZipArchiveMode.Create))
        {
            var manifest = zip.CreateEntry(ManifestName);
            using (var stream = manifest.Open())
            {
                JsonSerializer.Serialize(stream, new { site = "Sumlers Construction", createdAt = clock.GetUtcNow(), reason });
            }
            foreach (var (fullPath, entryName) in ContentFilesOnDisk())
            {
                zip.CreateEntryFromFile(fullPath, entryName, CompressionLevel.Fastest);
            }
        }
        File.Move(tempPath, path);
        if (prune) Prune();
        logger.LogInformation("Created backup {Path}", path);
        return ToInfo(new FileInfo(path));
    }

    private IEnumerable<(string FullPath, string EntryName)> ContentFilesOnDisk()
    {
        foreach (var name in _ContentFiles)
        {
            var path = dataFolder.PathFor(name);
            if (File.Exists(path)) yield return (path, name);
        }
        var uploads = dataFolder.PathFor(UploadsFolder);
        if (!Directory.Exists(uploads)) yield break;
        foreach (var path in Directory.EnumerateFiles(uploads, "*", SearchOption.AllDirectories))
        {
            yield return (path, $"{UploadsFolder}/{Path.GetRelativePath(uploads, path).Replace('\\', '/')}");
        }
    }

    // Only our own files, no folders outside uploads/, nothing absolute or with "..", and a sane total size.
    private static void EnsureValid(ZipArchive zip)
    {
        if (zip.GetEntry(ManifestName) is null) throw new InvalidDataException("This file is not a backup from this website.");
        if (zip.Entries.Count > 20_000) throw new InvalidDataException("The backup has too many files.");
        if (zip.Entries.Sum(e => e.Length) > MaxImportBytes * 2) throw new InvalidDataException("The backup is too large.");
        var bad = zip.Entries.FirstOrDefault(e => !IsAllowedEntry(e.FullName));
        if (bad is not null) throw new InvalidDataException($"The backup contains an unexpected file: {bad.FullName}");
    }

    private static bool IsAllowedEntry(string name)
    {
        if (name == ManifestName || _ContentFiles.Contains(name)) return true;
        if (!name.StartsWith(UploadsFolder + "/", StringComparison.Ordinal)) return false;
        var rest = name[(UploadsFolder.Length + 1)..].TrimEnd('/');
        if (rest.Length == 0) return true;
        return !rest.Contains('\\') && !rest.Contains(':') && rest.Split('/').All(part => part is not ("" or "." or ".."));
    }

    private void ReplaceContentWith(string backupPath)
    {
        foreach (var name in _ContentFiles) File.Delete(dataFolder.PathFor(name));
        var uploads = dataFolder.PathFor(UploadsFolder);
        Directory.CreateDirectory(uploads);
        foreach (var file in Directory.EnumerateFiles(uploads, "*", SearchOption.AllDirectories)) File.Delete(file);

        using var zip = ZipFile.OpenRead(backupPath);
        foreach (var entry in zip.Entries.Where(e => e.FullName != ManifestName && !e.FullName.EndsWith('/')))
        {
            var target = Path.GetFullPath(Path.Combine(dataFolder.Root, entry.FullName));
            if (!target.StartsWith(dataFolder.Root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) continue;
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            entry.ExtractToFile(target, overwrite: true);
        }
    }

    private void Prune()
    {
        foreach (var old in List().Skip(KeepCount))
        {
            File.Delete(Path.Combine(BackupDirectory, old.Name));
        }
    }

    private string NewName(string reason) => $"{clock.GetUtcNow():yyyyMMdd-HHmmssfff}-{reason}.zip";

    private static BackupInfo ToInfo(FileInfo file)
    {
        var reason = file.Name[19..^4];
        return new BackupInfo(file.Name, new DateTimeOffset(file.LastWriteTimeUtc, TimeSpan.Zero), file.Length, reason);
    }

    [GeneratedRegex(@"^\d{8}-\d{9}-[a-z-]+\.zip$")]
    private static partial Regex BackupName();
}
