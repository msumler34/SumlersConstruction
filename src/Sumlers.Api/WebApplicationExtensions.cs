using Microsoft.Extensions.FileProviders;
using Sumlers.Api.Admin;
using Sumlers.Api.Backups;
using Sumlers.Api.Contact;
using Sumlers.Api.Data;
using Sumlers.Api.Health;
using Sumlers.Api.Messages;
using Sumlers.Api.Settings;
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
        app.UseMiddleware<MainAddressRedirect>();
        app.UseStaticFiles();
        app.UseUploadedFiles();
        app.UseRateLimiter();

        app.MapSiteEndpoints();
        app.MapContactEndpoints();
        app.MapAdminEndpoints();
        app.MapSettingsEndpoints();
        app.MapMessageEndpoints();
        app.MapBackupEndpoints();
        app.MapSumlersHealthChecks();
        app.MapGet("/api/{**path}", () => Results.NotFound());

        // Every page address serves the Angular app, with that page's title and description already in it.
        app.MapPageEndpoints();
        return app;
    }

    private static void UseUploadedFiles(this WebApplication app)
    {
        var directory = AdminEndpoints.UploadsDirectory(app.Services.GetRequiredService<AppDataFolder>());
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
