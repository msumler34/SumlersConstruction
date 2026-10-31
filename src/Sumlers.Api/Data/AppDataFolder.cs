using Microsoft.Extensions.Options;
using Sumlers.Api.Options;

namespace Sumlers.Api.Data;

/// <summary>
/// The App_Data folder: everything the admin edits (content, settings, messages, photos) plus backups.
/// It is not part of a publish, so deployments never overwrite it.
/// </summary>
public sealed class AppDataFolder(IOptions<AdminOptions> options, IWebHostEnvironment environment)
{
    public string Root { get; } = Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.Value.DataDirectory));

    public string PathFor(string relativePath) => Path.Combine(Root, relativePath);
}
