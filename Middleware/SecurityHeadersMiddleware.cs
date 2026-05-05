namespace TechNova_IT_Solutions.Middleware;

/// <summary>
/// Middleware that adds security headers to all HTTP responses to protect against common web vulnerabilities.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add Content-Security-Policy header to prevent XSS attacks
        // Allows inline scripts for existing role UIs that use inline handlers and page scripts.
        // CDN sources are included for Bootstrap/Font Awesome assets used in Razor Pages.
        context.Response.Headers.Append("Content-Security-Policy",
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
            "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
            "img-src 'self' data:; " +
            "font-src 'self' https://cdnjs.cloudflare.com; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none'");

        // Add X-Frame-Options header to prevent clickjacking attacks
        context.Response.Headers.Append("X-Frame-Options", "DENY");

        // Add X-Content-Type-Options header to prevent MIME-sniffing attacks
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

        // Add Strict-Transport-Security header for HTTPS connections only
        // Enforces HTTPS for 1 year including all subdomains
        if (context.Request.IsHttps)
        {
            context.Response.Headers.Append("Strict-Transport-Security",
                "max-age=31536000; includeSubDomains");
        }

        // Add Referrer-Policy header to control referrer information leakage
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // Add Permissions-Policy header to restrict access to browser features
        context.Response.Headers.Append("Permissions-Policy",
            "geolocation=(), microphone=(), camera=()");

        // Call the next middleware in the pipeline
        await _next(context);
    }
}

/// <summary>
/// Extension method to register SecurityHeadersMiddleware in the application pipeline.
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
