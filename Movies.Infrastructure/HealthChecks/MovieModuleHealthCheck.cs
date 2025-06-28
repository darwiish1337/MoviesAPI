using Microsoft.Extensions.Diagnostics.HealthChecks;
using Movies.Application.Abstractions.Persistence;

namespace Movies.Infrastructure.HealthChecks;

public class MovieModuleHealthCheck(IMovieRepository movieRepository, IMovieImageRepository imageRepository,
    IRatingRepository ratingRepository) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var issues = new List<string>();

        try
        {
            var movieCheck = await movieRepository.ExistsAsync("dummy-title", 2000, cancellationToken);
        }
        catch (Exception ex)
        {
            issues.Add($"MovieRepository error: {ex.Message}");
        }

        try
        {
            var imageCheck = await imageRepository.GetAllAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            issues.Add($"ImageRepository error: {ex.Message}");
        }

        try
        {
            var ratingCheck = await ratingRepository.GetRatingAsync(Guid.NewGuid(), cancellationToken);
        }
        catch (Exception ex)
        {
            issues.Add($"RatingRepository error: {ex.Message}");
        }

        return issues.Any()
            ? HealthCheckResult.Unhealthy("Movie Module check failed:\n" + string.Join("\n", issues))
            : HealthCheckResult.Healthy("Movie Module is healthy.");
    }
}