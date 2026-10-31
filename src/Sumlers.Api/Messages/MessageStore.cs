using System.Text.Json.Serialization;
using Sumlers.Api.Contact;
using Sumlers.Api.Data;

namespace Sumlers.Api.Messages;

[JsonConverter(typeof(JsonStringEnumConverter<EmailStatus>))]
public enum EmailStatus
{
    Pending,
    Sent,
    Failed,
}

/// <summary>A contact form submission, kept even when the email about it could not be sent.</summary>
public sealed record ContactMessage
{
    public string Id { get; init; } = "";
    public DateTimeOffset ReceivedAt { get; init; }
    public string Name { get; init; } = "";
    public string Email { get; init; } = "";
    public string Phone { get; init; } = "";
    public string Service { get; init; } = "";
    public string Address { get; init; } = "";
    public string Message { get; init; } = "";
    public EmailStatus EmailStatus { get; init; }
    public bool IsRead { get; init; }

    public static ContactMessage From(ContactRequest request, DateTimeOffset receivedAt) => new()
    {
        Id = Guid.NewGuid().ToString("N")[..12],
        ReceivedAt = receivedAt,
        Name = request.Name.Trim(),
        Email = request.Email.Trim(),
        Phone = request.Phone?.Trim() ?? "",
        Service = request.Service?.Trim() ?? "",
        Address = request.Address?.Trim() ?? "",
        Message = request.Message.Trim(),
    };
}

public interface IMessageStore
{
    Task<IReadOnlyList<ContactMessage>> GetAllAsync(CancellationToken cancellationToken);
    Task AddAsync(ContactMessage message, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(string id, Func<ContactMessage, ContactMessage> change, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken);
}

/// <summary>Keeps contact messages in App_Data/messages.json, newest first, up to <see cref="MaxMessages"/>.</summary>
public sealed class JsonMessageStore(AppDataFolder dataFolder) : IMessageStore, IReloadable
{
    public const int MaxMessages = 2000;

    private readonly JsonDocument<List<ContactMessage>> _Document =
        new(dataFolder.PathFor("messages.json"), _ => Task.FromResult(new List<ContactMessage>()));

    public async Task<IReadOnlyList<ContactMessage>> GetAllAsync(CancellationToken cancellationToken) =>
        [.. await _Document.GetAsync(cancellationToken)];

    public Task AddAsync(ContactMessage message, CancellationToken cancellationToken) =>
        _Document.UpdateAsync(list =>
        {
            list.Insert(0, message);
            if (list.Count > MaxMessages) list.RemoveRange(MaxMessages, list.Count - MaxMessages);
            return true;
        }, cancellationToken);

    public Task<bool> UpdateAsync(string id, Func<ContactMessage, ContactMessage> change, CancellationToken cancellationToken) =>
        _Document.UpdateAsync(list =>
        {
            var index = list.FindIndex(m => m.Id == id);
            if (index < 0) return false;
            list[index] = change(list[index]);
            return true;
        }, cancellationToken);

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken) =>
        _Document.UpdateAsync(list => list.RemoveAll(m => m.Id == id) > 0, cancellationToken);

    public void Reload() => _Document.Reload();
}
