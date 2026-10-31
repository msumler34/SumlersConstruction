using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Sumlers.Api.Options;

namespace Sumlers.Api.Admin;

/// <summary>Allows a request only when its X-Admin-Key header matches Admin:ApiKey.</summary>
public sealed class AdminKeyFilter(IOptions<AdminOptions> options) : IEndpointFilter
{
    public const string HeaderName = "X-Admin-Key";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var supplied = context.HttpContext.Request.Headers[HeaderName].ToString();
        return IsValid(supplied) ? await next(context) : Results.Unauthorized();
    }

    private bool IsValid(string supplied)
    {
        var expected = options.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrEmpty(supplied)) return false;

        // Compare hashes in fixed time, so response timing does not leak how much of the key matched.
        var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(supplied));
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
        return CryptographicOperations.FixedTimeEquals(suppliedHash, expectedHash);
    }
}
