using HealthChecks.UI.Client;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Movies.Domain.Constants;

namespace Movies.Presentation.Extension;

public static class HealthCheckEndpointExtensions
{
    public static IEndpointRouteBuilder MapDynamicHealthCheck(this IEndpointRouteBuilder app)
    {
        app.MapGet(HealthCheckConstants.HealthCheckEndpoint, async (HttpContext context, HealthCheckService healthCheckService) =>
        {
            var tag = context.Request.Query["tag"].ToString();

            var tagSetCache = new Dictionary<HealthCheckRegistration, HashSet<string>>();

            var report = await healthCheckService.CheckHealthAsync(hc =>
            {
                if (string.IsNullOrWhiteSpace(tag))
                    return true;

                if (!tagSetCache.TryGetValue(hc, out var tagSet))
                {
                    tagSet = new HashSet<string>(hc.Tags, StringComparer.OrdinalIgnoreCase);
                    tagSetCache[hc] = tagSet;
                }

                return tagSet.Contains(tag);
            });

            context.Response.ContentType = "application/json";
            await UIResponseWriter.WriteHealthCheckUIResponse(context, report);
        });

        return app;
    }
}
