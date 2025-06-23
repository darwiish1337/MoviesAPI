using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Movies.Application.Abstractions.Persistence;
using Movies.Application.Abstractions.Services;
using Movies.Domain.Models;

namespace Movies.Infrastructure.BackgroundServices;

public class ImageCleanupService(IServiceProvider serviceProvider, ILogger<ImageCleanupService> logger)
    : BackgroundService
{
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(24); // Run daily

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupOrphanedImagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred during image cleanup");
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }
    }
    
    private async Task CleanupOrphanedImagesAsync(CancellationToken cancellationToken)
    {
        var services = GetCleanupServices();
    
        try
        {
            logger.LogInformation("Starting orphaned images cleanup");
        
            var allImages = await services.ImageRepository.GetAllAsync(cancellationToken);
            var orphanedImages = new List<MovieImage>();
        
            foreach (var image in allImages)
            {
                var movieExists = await services.MovieRepository.ExistsByIdAsync(image.MovieId, cancellationToken);
                if (!movieExists)
                {
                    orphanedImages.Add(image);
                }
            }
            
            foreach (var orphanedImage in orphanedImages)
            {
                await services.CloudinaryService.DeleteImageAsync(orphanedImage.PublicId, cancellationToken);
                
                await services.ImageRepository.DeleteAsync(orphanedImage.Id, cancellationToken);
            }
        
            logger.LogInformation("Orphaned images cleanup completed. Removed {Count} images", orphanedImages.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during orphaned images cleanup");
            throw;
        }
    }

    private CleanupServices GetCleanupServices()
    {
        using var scope = serviceProvider.CreateScope();
        return new CleanupServices(
            scope.ServiceProvider.GetRequiredService<IMovieImageRepository>(),
            scope.ServiceProvider.GetRequiredService<IMovieRepository>(),
            scope.ServiceProvider.GetRequiredService<ICloudinaryService>()
        );
    }
    
    private record CleanupServices(
        IMovieImageRepository ImageRepository,
        IMovieRepository MovieRepository,
        ICloudinaryService CloudinaryService);
}