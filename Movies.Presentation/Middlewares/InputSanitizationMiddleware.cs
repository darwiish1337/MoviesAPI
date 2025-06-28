using System.Text;
using Movies.Application.Helpers;
using Movies.Domain.Constants;

namespace Movies.Presentation.Middlewares;

public class InputSanitizationMiddleware(RequestDelegate next, ILogger<InputSanitizationMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // 🔹 Check JSON Body
        if (context.Request.HasJsonContentType())
        {
            context.Request.EnableBuffering();

            using var reader = new StreamReader(
                context.Request.Body,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024,
                leaveOpen: true);

            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            if (SanitizationHelper.ContainsDangerousContent(body))
            {
                logger.LogWarning("Unsafe body content detected: {Body}", body);
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("The request body contains unsafe content.");
                return;
            }
        }

        // 🔹 Check Query String (with allowlist for safe keys)
        var safeKeys = SanitizationConstants.SafeQueryKeys;

        foreach (var (key, values) in context.Request.Query)
        {
            // لو المفتاح غير موجود في القائمة، ارفض حتى لو بدون قيمة
            if (!safeKeys.Contains(key, StringComparer.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync($"Unsafe or unsupported query parameter '{key}'.");
                return;
            }

            // ولو فيه قيمة، شيك هل فيها محتوى خطير
            foreach (var value in values)
            {
                if (value != null && SanitizationHelper.ContainsDangerousContent(value))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync($"Unsafe content in query parameter '{key}'.");
                    return;
                }
            }
        }
        
        await next(context);
    }
}