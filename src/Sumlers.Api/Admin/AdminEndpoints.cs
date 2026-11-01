using Microsoft.Extensions.Options;
using Sumlers.Api.Backups;
using Sumlers.Api.Data;
using Sumlers.Api.Images;
using Sumlers.Api.Options;
using Sumlers.Api.Promotions;

namespace Sumlers.Api.Admin;

public static class AdminEndpoints
{
    private static readonly HashSet<string> _ImageTypes = new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };

    // Phone photos are often 4000 pixels wide and several MB; no page shows a photo bigger than this.
    private const int MaxPhotoSide = 2560;

    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/admin").AddEndpointFilter<AdminKeyFilter>();
        admin.MapGet("/verify", () => Results.NoContent());
        admin.MapGet("/promotions", async (IPromotionStore store, CancellationToken ct) => Results.Ok(await store.GetAllAsync(ct)));

        var changes = admin.MapGroup("/promotions").AddEndpointFilter<SnapshotBeforeChangeFilter>();
        changes.MapPost("/", SaveAsync);
        changes.MapPut("/{id}", (string id, Promotion promotion, IPromotionStore store, CancellationToken ct) =>
            SaveAsync(promotion with { Id = id }, store, ct));
        changes.MapDelete("/{id}", async (string id, IPromotionStore store, CancellationToken ct) =>
            await store.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound());

        admin.MapPost("/uploads", UploadAsync).DisableAntiforgery();
    }

    private static async Task<IResult> SaveAsync(Promotion promotion, IPromotionStore store, CancellationToken ct)
    {
        var errors = PromotionValidator.Validate(promotion);
        if (errors.Count > 0) return Results.ValidationProblem(errors);
        return Results.Ok(await store.SaveAsync(promotion, ct));
    }

    private static async Task<IResult> UploadAsync(IFormFile file, IOptions<AdminOptions> options, AppDataFolder dataFolder, CancellationToken ct)
    {
        if (file.Length == 0 || file.Length > options.Value.MaxUploadBytes)
        {
            return Results.BadRequest(new { error = $"Images must be smaller than {options.Value.MaxUploadBytes / 1024 / 1024} MB." });
        }
        await using var source = file.OpenReadStream();
        if (!_ImageTypes.Contains(file.ContentType) || PhotoResizer.ToWebP(source, quality: 80, MaxPhotoSide, MaxPhotoSide) is not { } webp)
        {
            return Results.BadRequest(new { error = "Upload a JPG, PNG or WebP image." });
        }

        // The server picks the file name, so an uploaded name can never escape the uploads folder.
        var fileName = $"{Guid.NewGuid():N}.webp";
        var directory = UploadsDirectory(dataFolder);
        Directory.CreateDirectory(directory);
        await File.WriteAllBytesAsync(Path.Combine(directory, fileName), webp, ct);
        return Results.Ok(new { url = $"/uploads/{fileName}" });
    }

    public static string UploadsDirectory(AppDataFolder dataFolder) => dataFolder.PathFor("uploads");
}
