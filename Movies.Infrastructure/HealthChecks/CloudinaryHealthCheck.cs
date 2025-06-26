using Microsoft.Extensions.Diagnostics.HealthChecks;
using Movies.Application.Abstractions.Services;

namespace Movies.Infrastructure.HealthChecks;

public class CloudinaryHealthCheck(ICloudinaryService cloudinaryService) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await cloudinaryService.GetImageDetailsAsync("non-existent-test-id", cancellationToken);

            return HealthCheckResult.Healthy("Cloudinary is accessible");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Cloudinary is not accessible", ex);
        }
    }
}