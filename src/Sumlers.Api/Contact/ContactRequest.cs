using System.Net.Mail;

namespace Sumlers.Api.Contact;

public sealed record ContactRequest(
    string Name,
    string Email,
    string? Phone,
    string? Service,
    string? Address,
    string Message,
    string? Website)
{
    /// <summary>Website is a hidden honeypot field. People never see it, so only bots fill it in.</summary>
    public bool IsLikelySpam => !string.IsNullOrWhiteSpace(Website);

    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        AddIf(errors, string.IsNullOrWhiteSpace(Name) || Name.Length > 100, nameof(Name), "Please enter your name (100 characters max).");
        AddIf(errors, !IsValidEmail(Email), nameof(Email), "Please enter a valid email address.");
        AddIf(errors, Phone?.Length > 40, nameof(Phone), "Phone number is too long.");
        AddIf(errors, Service?.Length > 80, nameof(Service), "Service is too long.");
        AddIf(errors, Address?.Length > 200, nameof(Address), "Address is too long.");
        AddIf(errors, string.IsNullOrWhiteSpace(Message) || Message.Length > 4000, nameof(Message), "Please tell us about your project (4000 characters max).");
        return errors;
    }

    private static bool IsValidEmail(string? email) =>
        !string.IsNullOrWhiteSpace(email) && email.Length <= 200 && MailAddress.TryCreate(email, out var parsed) && parsed.Address == email.Trim();

    private static void AddIf(Dictionary<string, string[]> errors, bool condition, string field, string message)
    {
        if (condition) errors[field] = [message];
    }
}
