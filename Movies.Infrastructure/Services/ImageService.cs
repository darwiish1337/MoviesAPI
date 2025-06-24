using FluentValidation;
using Microsoft.Extensions.Logging;
using Movies.Application.Abstractions.Caching;
using Movies.Application.Abstractions.Persistence;
using Movies.Application.Abstractions.Services;
using Movies.Application.DTOs.Requests;
using Movies.Application.DTOs.Responses;
using Movies.Application.Mapping;
using Movies.Domain.Constants;
using Movies.Domain.Exceptions;
using Movies.Domain.Models;
using Movies.Domain.ValueObjects;

namespace Movies.Infrastructure.Services;

public class ImageService(IMovieImageRepository imageRepository, IMovieRepository movieRepository, ICloudinaryService cloudinaryService,
    ICacheService cacheService, ILogger<ImageService> logger, IValidator<ImageUploadRequest> imageUploadValidator) : IImageService
{
    public async Task<ImageResponse> UploadImageAsync(ImageUploadRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting image upload for movie {MovieId}", request.MovieId);

        // Validate request
        await imageUploadValidator.ValidateAndThrowAsync(request, cancellationToken);

        // Validate movie exists
        if (!await movieRepository.ExistsByIdAsync(request.MovieId, cancellationToken))
        {
            throw new ImageUploadException("Movie not found");
        }

        try
        {
            // Upload to Cloudinary
            await using var stream = request.File.OpenReadStream();
            var fileName = Path.GetFileNameWithoutExtension(request.File.FileName);
            var folder = $"movies/{request.MovieId}";

            var uploadResult = await cloudinaryService.UploadImageAsync(stream, fileName, folder, cancellationToken);

            if (uploadResult.Error != null)
            {
                throw new ImageUploadException($"Cloudinary upload failed: {uploadResult.Error.Message}");
            }

            // Generate transformed URLs
            var originalUrl = cloudinaryService.GenerateUrl(uploadResult.PublicId);
            var thumbnailUrl = cloudinaryService.GenerateUrl(uploadResult.PublicId, ImageTransformation.Thumbnail);
            var mediumUrl = cloudinaryService.GenerateUrl(uploadResult.PublicId, ImageTransformation.Medium);
            var largeUrl = cloudinaryService.GenerateUrl(uploadResult.PublicId, ImageTransformation.Large);

            // Create a domain model
            var movieImage = new MovieImage
            {
                Id = Guid.NewGuid(),
                MovieId = request.MovieId,
                PublicId = uploadResult.PublicId,
                OriginalUrl = originalUrl,
                ThumbnailUrl = thumbnailUrl,
                MediumUrl = mediumUrl,
                LargeUrl = largeUrl,
                AltText = request.AltText,
                Width = uploadResult.Width,
                Height = uploadResult.Height,
                Size = uploadResult.Bytes,
                Format = uploadResult.Format,
                IsPrimary = request.IsPrimary,
                CreatedAt = DateTime.UtcNow
            };

            // Handle primary image logic
            if (request.IsPrimary)
            {
                await imageRepository.SetPrimaryImageAsync(movieImage.Id, request.MovieId, cancellationToken);
            }

            // Save to a database
            var success = await imageRepository.CreateAsync(movieImage, cancellationToken);
            if (!success)
            {
                // Cleanup Cloudinary if DB save failed
                await cloudinaryService.DeleteImageAsync(uploadResult.PublicId, cancellationToken);
                throw new ImageUploadException("Failed to save image to database");
            }

            // Cache the image
            var cacheKey = $"{ImageConstants.CacheKeyPrefix}:{movieImage.Id}";
            await cacheService.SetAsync(cacheKey, movieImage, TimeSpan.FromHours(24), cancellationToken);

            // Invalidate movie images cache
            await cacheService.RemoveByPatternAsync($"{ImageConstants.CacheKeyPrefix}:movie:{request.MovieId}:*", cancellationToken);

            logger.LogInformation("Image uploaded successfully with ID {ImageId}", movieImage.Id);
            
            return movieImage.MapToResponse();
        }
        catch (Exception ex) when (!(ex is ImageUploadException))
        {
            logger.LogError(ex, "Unexpected error during image upload");
            throw new ImageUploadException("An unexpected error occurred during image upload", ex);
        }
    }

    public async Task<bool> DeleteImageAsync(Guid imageId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting image {ImageId}", imageId);

        var image = await imageRepository.GetByIdAsync(imageId, cancellationToken);
        if (image == null)
        {
            return false;
        }

        try
        {
            // Delete it from Cloudinary
            var deleteResult = await cloudinaryService.DeleteImageAsync(image.PublicId, cancellationToken);

            if (deleteResult.Error != null)
            {
                logger.LogError($"Failed to delete image from Cloudinary: {deleteResult.Error}");
                return false;
            }

            // Delete it from a database even if Cloudinary deletion failed (for cleanup)
            var dbDeleteResult = await imageRepository.DeleteAsync(imageId, cancellationToken);

            // Remove from cache
            var cacheKey = $"{ImageConstants.CacheKeyPrefix}:{imageId}";
            await cacheService.RemoveAsync(cacheKey, cancellationToken);

            // Invalidate movie images cache
            await cacheService.RemoveByPatternAsync($"{ImageConstants.CacheKeyPrefix}:movie:{image.MovieId}:*", cancellationToken);

            logger.LogInformation("Image {ImageId} deleted successfully", imageId);
            return dbDeleteResult;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting image {ImageId}", imageId);
            throw;
        }
    }

    public async Task<ImageResponse?> GetImageAsync(Guid imageId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{ImageConstants.CacheKeyPrefix}:{imageId}";

        // Try cache first
        var cachedImage = await cacheService.GetAsync<MovieImage>(cacheKey, cancellationToken);
        if (cachedImage != null)
        {
            return cachedImage.MapToResponse();
        }

        // Get from a database
        var image = await imageRepository.GetByIdAsync(imageId, cancellationToken);
        if (image == null)
        {
            return null;
        }

        // Cache for next time
        await cacheService.SetAsync(cacheKey, image, TimeSpan.FromHours(24), cancellationToken);
        
        return image.MapToResponse();
    }

    public async Task<IEnumerable<ImageResponse>> GetMovieImagesAsync(Guid movieId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{ImageConstants.CacheKeyPrefix}:movie:{movieId}:all";

        // Try cache first
        var cachedImages = await cacheService.GetAsync<List<MovieImage>>(cacheKey, cancellationToken);
        if (cachedImages != null)
        {
            return cachedImages.Select(x => x.MapToResponse());
        }

        // Get from a database
        var images = await imageRepository.GetByMovieIdAsync(movieId, cancellationToken);
        var imageList = images.ToList();

        // Cache for next time
        await cacheService.SetAsync(cacheKey, imageList, TimeSpan.FromHours(12), cancellationToken);
        
        return imageList.Select(x => x.MapToResponse());
    }

    public async Task<bool> SetPrimaryImageAsync(Guid imageId, CancellationToken cancellationToken = default)
    {
        var image = await imageRepository.GetByIdAsync(imageId, cancellationToken);
        if (image == null)
        {
            return false;
        }

        var result = await imageRepository.SetPrimaryImageAsync(imageId, image.MovieId, cancellationToken);

        if (result)
        {
            // Invalidate cache
            await cacheService.RemoveByPatternAsync($"{ImageConstants.CacheKeyPrefix}:movie:{image.MovieId}:*", cancellationToken);
        }

        return result;
    }

    public async Task<ImageResponse> UpdateImageAsync(Guid imageId, ImageUploadRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Updating image {ImageId}", imageId);

        // Validate request
        await imageUploadValidator.ValidateAndThrowAsync(request, cancellationToken);

        var existingImage = await imageRepository.GetByIdAsync(imageId, cancellationToken);
        if (existingImage == null)
        {
            throw new ImageUploadException("Image not found");
        }

        if (existingImage.MovieId != request.MovieId)
        {
            throw new ImageUploadException("Image does not belong to the specified movie");
        }

        try
        {
            // Delete old image from Cloudinary
            var deleteResult = await cloudinaryService.DeleteImageAsync(existingImage.PublicId, cancellationToken);
            if (deleteResult.Error != null)
            {
                logger.LogWarning("Failed to delete previous image from Cloudinary: {Error}", deleteResult.Error);
            }

            // Upload new image
            await using var stream = request.File.OpenReadStream();
            var fileName = Path.GetFileNameWithoutExtension(request.File.FileName);
            var folder = $"movies/{request.MovieId}";

            var uploadResult = await cloudinaryService.UploadImageAsync(stream, fileName, folder, cancellationToken);
            if (uploadResult.Error != null)
            {
                throw new ImageUploadException($"Cloudinary upload failed: {uploadResult.Error.Message}");
            }

            var updatedImage = new MovieImage
            {
                Id = existingImage.Id,
                MovieId = existingImage.MovieId,
                PublicId = uploadResult.PublicId,
                OriginalUrl = cloudinaryService.GenerateUrl(uploadResult.PublicId),
                ThumbnailUrl = cloudinaryService.GenerateUrl(uploadResult.PublicId, ImageTransformation.Thumbnail),
                MediumUrl = cloudinaryService.GenerateUrl(uploadResult.PublicId, ImageTransformation.Medium),
                LargeUrl = cloudinaryService.GenerateUrl(uploadResult.PublicId, ImageTransformation.Large),
                AltText = request.AltText,
                Width = uploadResult.Width,
                Height = uploadResult.Height,
                Size = uploadResult.Bytes,
                Format = uploadResult.Format,
                IsPrimary = request.IsPrimary,
                CreatedAt = existingImage.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };

            // Update DB
            var success = await imageRepository.UpdateAsync(updatedImage, cancellationToken);
            if (!success)
            {
                throw new ImageUploadException("Failed to update image in the database");
            }

            // Handle primary
            if (request.IsPrimary)
            {
                await imageRepository.SetPrimaryImageAsync(updatedImage.Id, updatedImage.MovieId, cancellationToken);
            }

            // Update cache
            var cacheKey = $"{ImageConstants.CacheKeyPrefix}:{updatedImage.Id}";
            await cacheService.SetAsync(cacheKey, updatedImage, TimeSpan.FromHours(24), cancellationToken);
            await cacheService.RemoveByPatternAsync($"{ImageConstants.CacheKeyPrefix}:movie:{updatedImage.MovieId}:*", cancellationToken);

            logger.LogInformation("Image {ImageId} updated successfully", updatedImage.Id);
            
            return updatedImage.MapToResponse();
        }
        catch (Exception ex) when (ex is not ImageUploadException)
        {
            logger.LogError(ex, "Unexpected error while updating image {ImageId}", imageId);
            throw new ImageUploadException("An unexpected error occurred while updating the image", ex);
        }
    }

    public string GenerateTransformedUrlAsync(string publicId, ImageTransformation transformation)
    {
        return cloudinaryService.GenerateUrl(publicId, transformation);
    }
    
}
