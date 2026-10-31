namespace Sumlers.Api.Data;

/// <summary>Collects field errors in the shape Results.ValidationProblem expects.</summary>
public sealed class ValidationErrors : Dictionary<string, string[]>
{
    public ValidationErrors AddIf(bool condition, string field, string message)
    {
        if (condition) this[field] = [message];
        return this;
    }

    // Blocks javascript: and data: links, since admins paste these URLs straight into the page.
    public static bool IsSafeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        string[] allowed = ["https://", "http://", "mailto:", "tel:", "/"];
        return allowed.Any(prefix => url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) && !url.StartsWith("//");
    }

    public static bool IsHttpsUrl(string? url) =>
        string.IsNullOrWhiteSpace(url) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
}
