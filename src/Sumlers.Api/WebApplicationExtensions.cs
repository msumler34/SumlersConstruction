using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Sumlers.Api.Admin;
using Sumlers.Api.Contact;
using Sumlers.Api.Options;
using Sumlers.Api.Site;

namespace Sumlers.Api;

public static class WebApplicationExtensions
{
    public static WebApplication UseSumlersPipeline(this WebApplication app)
    {
        app.UseForwardedHeaders();
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        if (!app.Environment.IsDevelopment()) app.UseHsts();

        app.Use(AddSecurityHeaders);
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseUploadedFiles();
        app.UseRateLimiter();

        app.MapSiteEndpoints();
        app.MapContactEndpoints();
        app.MapAdminEndpoints();
        app.MapGet("/api/{**path}", () => Results.NotFound());

        // Angular handles client-side routes such as /services and /contact.
        app.MapFallbackToFile("index.html");
        return app;
    }

    private static void UseUploadedFiles(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<AdminOptions>>().Value;
        var directory = AdminEndpoints.UploadsDirectory(app.Environment, options);
        Directory.CreateDirectory(directory);
        app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(directory), RequestPath = "/uploads" });
    }

    private static Task AddSecurityHeaders(HttpContext context, Func<Task> next)
    {
        var headers = context.Response.Headers;
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        return next();
    }
}
