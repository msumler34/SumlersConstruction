namespace Sumlers.Api.Site;

/// <summary>
/// /index.html is only the Angular build's template (no page title, text or business details), so anyone or any
/// search engine that asks for it is sent to the real home page instead.
/// </summary>
public static class IndexHtmlRedirect
{
    public static Task InvokeAsync(HttpContext context, Func<Task> next)
    {
        var path = context.Request.Path.Value ?? "";
        if (path.Equals("/index.html", StringComparison.OrdinalIgnoreCase) || path.Equals("/index.htm", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Redirect("/" + context.Request.QueryString, permanent: true);
            return Task.CompletedTask;
        }
        return next();
    }
}
