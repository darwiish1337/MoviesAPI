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

public class BulkImageService(IMovieImageRepository imageRepository, IMovieRepository movieRepository, ICloudinaryService cloudinaryService,
    ICacheService cacheService, IValidator<ImageUploadRequest> imageUploadValidator, ILogger<BulkImageService> logger) : IBulkImageService
{
    public async Task<IEnumerable<ImageResponse>> UploadImagesAsync(IEnumerable<ImageUploadRequest> requests, CancellationToken cancellationToken = default)
    {
        var uploadedImages = new List<ImageResponse>();

        foreach (var request in requests)
        {
            try
            {
                await imageUploadValidator.ValidateAndThrowAsync(request, cancellationToken);

                if (!await movieRepository.ExistsByIdAsync(request.MovieId, cancellationToken))
                    throw new ImageUploadException("Movie not found");

                await using var stream = request.File.OpenReadStream();
                var fileName = Path.GetFileNameWithoutExtension(request.File.FileName);
                var folder = $"movies/{request.MovieId}";

                var uploadResult = await cloudinaryService.UploadImageAsync(stream, fileName, folder, cancellationToken);
                if (uploadResult.Error != null)
                    throw new ImageUploadException($"Cloudinary error: {uploadResult.Error}");

                var image = new MovieImage
                {
                    Id = Guid.NewGuid(),
                    MovieId = request.MovieId,
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
                    CreatedAt = DateTime.UtcNow
                };

                var success = await imageRepository.CreateManyAsync([image], cancellationToken);
                if (!success)
                {
                    await cloudinaryService.DeleteImageAsync(uploadResult.PublicId, cancellationToken);
                    throw new ImageUploadException("Failed to save image to DB");
                }

                if (request.IsPrimary)
                {
                    await imageRepository.SetPrimaryImageAsync(image.Id, image.MovieId, cancellationToken);
                }

                var cacheKey = $"{ImageConstants.CacheKeyPrefix}:{image.Id}";
                await cacheService.SetAsync(cacheKey, image, TimeSpan.FromHours(24), cancellationToken);
                await cacheService.RemoveByPatternAsync($"{ImageConstants.CacheKeyPrefix}:movie:{request.MovieId}:*", cancellationToken);

                logger.LogInformation("Uploaded image {ImageId} for movie {MovieId}", image.Id, image.MovieId);
                uploadedImages.Add(image.MapToResponse());
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to upload image for movie {MovieId}", request.MovieId);
            }
        }

        return uploadedImages;
    }

    public async Task<bool> DeleteImagesAsync(IEnumerable<Guid>? imageIds, CancellationToken cancellationToken = default)
    {
        var allSucceeded = true;

        if (imageIds == null)
            return allSucceeded;

        foreach (var id in imageIds)
        {
            try
            {
                var image = await imageRepository.GetByIdAsync(id, cancellationToken);
                if (image == null)
                {
                    logger.LogWarning("Image not found for ID {ImageId}", id);
                    allSucceeded = false;
                    continue;
                }

                var deleteResult = await cloudinaryService.DeleteImageAsync(image.PublicId, cancellationToken);
                if (deleteResult.Error != null)
                {
                    logger.LogError("Failed to delete image from Cloudinary: {Error}", deleteResult.Error);
                    allSucceeded = false;
                    continue;
                }

                var dbDelete = await imageRepository.DeleteManyAsync([id], cancellationToken);
                if (!dbDelete)
                {
                    logger.LogError("Failed to delete image from DB for ID {ImageId}", id);
                    allSucceeded = false;
                }

                var cacheKey = $"{ImageConstants.CacheKeyPrefix}:{image.Id}";
                await cacheService.RemoveAsync(cacheKey, cancellationToken);
                await cacheService.RemoveByPatternAsync($"{ImageConstants.CacheKeyPrefix}:movie:{image.MovieId}:*", cancellationToken);

                logger.LogInformation("Deleted image {ImageId} for movie {MovieId}", image.Id, image.MovieId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception deleting image ID {ImageId}", id);
                allSucceeded = false;
            }
        }

        return allSucceeded;
    }
    
    public async Task<bool> UpdateImagesAsync(IEnumerable<MovieImage> images, CancellationToken cancellationToken = default)
    {
        if (images == null || !images.Any())
            return false;

        // Validate all images first (optional)
        foreach (var image in images)
        {
            if (image.MovieId == Guid.Empty || image.Id == Guid.Empty)
                throw new ImageUploadException("Image ID and Movie ID must be valid.");
        }

        // Perform bulk update in DB
        var success = await imageRepository.UpdateManyAsync(images, cancellationToken);
        if (!success)
        {
            throw new ImageUploadException("Failed to update images in the database");
        }

        // Optional: Update cache for each image
        foreach (var image in images)
        {
            var cacheKey = $"{ImageConstants.CacheKeyPrefix}:{image.Id}";
            await cacheService.SetAsync(cacheKey, image, TimeSpan.FromHours(24), cancellationToken);

            // Invalidate related movie image group cache
            await cacheService.RemoveByPatternAsync($"{ImageConstants.CacheKeyPrefix}:movie:{image.MovieId}:*", cancellationToken);
        }

        logger.LogInformation("Successfully updated {Count} images", images.Count());

        return true;
    }
}