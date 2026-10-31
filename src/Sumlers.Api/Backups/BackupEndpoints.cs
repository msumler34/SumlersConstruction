using Microsoft.AspNetCore.Mvc;
using Sumlers.Api.Admin;

namespace Sumlers.Api.Backups;

public static class BackupEndpoints
{
    public static void MapBackupEndpoints(this IEndpointRouteBuilder app)
    {
        var backups = app.MapGroup("/api/admin/backups").AddEndpointFilter<AdminKeyFilter>();
        backups.MapGet("/", (BackupService service) => Results.Ok(service.List()));
        backups.MapPost("/", async (BackupService service, CancellationToken ct) => Results.Ok(await service.CreateAsync("manual", ct)));
        backups.MapGet("/{name}", (string name, BackupService service) =>
            service.PathOf(name) is { } path ? Results.File(path, "application/zip", name) : Results.NotFound());
        backups.MapPost("/{name}/restore", RestoreAsync);

        // The zip is sent as the raw request body, so the multipart form size limits do not apply.
        backups.MapPost("/upload", ImportAsync).WithMetadata(new RequestSizeLimitAttribute(BackupService.MaxImportBytes));
    }

    private static async Task<IResult> RestoreAsync(string name, BackupService service, CancellationToken ct)
    {
        if (service.PathOf(name) is null) return Results.NotFound();
        try
        {
            await service.RestoreAsync(name, ct);
            return Results.Ok(new { message = "Restored. A backup of what was there before was saved first." });
        }
        catch (InvalidDataException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> ImportAsync(HttpRequest request, BackupService service, CancellationToken ct)
    {
        try
        {
            return Results.Ok(await service.ImportAsync(request.Body, ct));
        }
        catch (InvalidDataException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}
