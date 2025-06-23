using System.Net;
using System.Text.Json;
using Movies.Domain.Exceptions;

namespace Movies.Presentation.Middlewares;

public class ImageUploadExceptionMiddleware(RequestDelegate next, ILogger<ImageUploadExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ImageUploadException ex)
        {
            logger.LogWarning(ex, "Image upload exception occurred");
            await HandleImageUploadExceptionAsync(context, ex);
        }
    }

    private static async Task HandleImageUploadExceptionAsync(HttpContext context, ImageUploadException ex)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

        var response = new
        {
            error = "Image Upload Failed",
            message = ex.Message,
            timestamp = DateTime.UtcNow
        };

        var jsonResponse = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(jsonResponse);
    }
}
