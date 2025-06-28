using Movies.Domain.Constants;

namespace Movies.Presentation.Middlewares;

public class CspMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers[HeaderNames.ContentSecurityPolicy] = CspConstants.ContentSecurityPolicy;
        
        await next(context);
    }
}
