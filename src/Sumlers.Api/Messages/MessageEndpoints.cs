using Sumlers.Api.Admin;

namespace Sumlers.Api.Messages;

public static class MessageEndpoints
{
    public static void MapMessageEndpoints(this IEndpointRouteBuilder app)
    {
        var messages = app.MapGroup("/api/admin/messages").AddEndpointFilter<AdminKeyFilter>();
        messages.MapGet("/", async (IMessageStore store, CancellationToken ct) => Results.Ok(await store.GetAllAsync(ct)));
        messages.MapPut("/{id}/read", async (string id, bool? isRead, IMessageStore store, CancellationToken ct) =>
            await store.UpdateAsync(id, m => m with { IsRead = isRead ?? true }, ct) ? Results.NoContent() : Results.NotFound());
        messages.MapDelete("/{id}", async (string id, IMessageStore store, CancellationToken ct) =>
            await store.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound());
    }
}
