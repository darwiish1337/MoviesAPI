namespace Movies.Domain.Constants;

public static class CspConstants
{
    public const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' https://res.cloudinary.com data:; " +
        "font-src 'self' https://fonts.gstatic.com; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "object-src 'none'; " +
        "upgrade-insecure-requests; " +
        "block-all-mixed-content;";
}

public static class HeaderNames
{
    public const string ContentSecurityPolicy = "Content-Security-Policy";
    
    public const string ContentSecurityPolicyReportOnly = "Content-Security-Policy-Report-Only";
}