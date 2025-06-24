using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Movies.Application.Abstractions.Services;
using Movies.Application.DTOs.Requests;
using Movies.Application.DTOs.Responses;
using Movies.Domain.Constants;
using Movies.Domain.Exceptions;

namespace Movies.Presentation.Controllers;

/// <summary>
/// Controller responsible for managing image-related operations for movies.
/// </summary>
/// <remarks>
/// This controller provides a complete set of endpoints for managing images associated with movies,
/// including uploading single or multiple images, retrieving images, updating, setting a primary image,
/// and deleting one or more images. 
///
/// All image operations are scoped to a specific movie and are protected by rate limiting policies. 
/// The controller supports large file uploads and enforces validation to ensure that image operations
/// are always associated with the correct movie.
///
/// Use this controller when building features like:
/// - Image galleries for movies
/// - Cover/primary image selection
/// - Media management dashboards
///
/// All endpoints return standardized HTTP responses, and errors are logged with contextual information.
/// </remarks>
[ApiController] 
[ApiVersion(1.0)]
[EnableRateLimiting(AuthConstants.ImageUploadPolicyName)]
public class MovieImagesController(IImageService imageService, IBulkImageService bulkImageService, ILogger<MovieImagesController> logger) : ControllerBase
{
    /// <summary>
    /// Uploads an image for a specific movie.
    /// </summary>
    /// <param name="id">The ID of the movie to which the image will be uploaded.</param>
    /// <param name="request">The image upload request containing the image file and related data.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// Returns <see cref="ActionResult{ImageResponse}"/> containing the created image details,
    /// or an error response in case of failure.
    /// </returns>
    [HttpPost(ApiEndpoints.MovieImages.Create)]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = 10_000_000)] // 10MB
    public async Task<ActionResult<ImageResponse>> UploadImage(Guid id, [FromForm] ImageUploadRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Ensure movieId from route matches request
            if (request.MovieId != id)
            {
                return BadRequest("Movie ID mismatch");
            }

            var result = await imageService.UploadImageAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetImage), new { Id = id, imageId = result.Id }, result);
        }
        catch (ImageUploadException ex)
        {
            logger.LogWarning(ex, "Image upload failed for movie {MovieId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error uploading image for movie {MovieId}", id);
            return StatusCode(500, "An error occurred while uploading the image");
        }
    }
    
    /// <summary>
    /// Uploads multiple images for a specific movie.
    /// </summary>
    /// <param name="id">The ID of the movie.</param>
    /// <param name="request">The bulk image upload request containing multiple image files.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list of uploaded image details.</returns>
    [HttpPost(ApiEndpoints.MovieImages.BulkCreate)]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = 50_000_000)] // 50MB
    public async Task<ActionResult<IEnumerable<ImageResponse>>> UploadImages(Guid id, [FromForm] BulkImageUploadRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Files.Count == 0)
        {
            return BadRequest("No files provided");
        }

        if (request.MovieId != id)
        {
            return BadRequest("Movie ID mismatch");
        }

        var uploadRequests = request.Files.Select(file => new ImageUploadRequest
        {
            MovieId = id,
            File = file,
            AltText = request.AltText,
            IsPrimary = false
        });

        var result = await bulkImageService.UploadMultipleImagesAsync(uploadRequests, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves all images associated with a specific movie.
    /// </summary>
    /// <param name="id">The ID of the movie.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list of images for the specified movie.</returns>
    [HttpGet(ApiEndpoints.MovieImages.GetMovieImages)]
    [EnableRateLimiting(AuthConstants.DefaultPolicyName)]
    public async Task<ActionResult<IEnumerable<ImageResponse>>> GetMovieImages(Guid id, CancellationToken cancellationToken)
    {
        var images = await imageService.GetMovieImagesAsync(id, cancellationToken);
        return Ok(images);
    }

    /// <summary>
    /// Retrieves a specific image by ID for a given movie.
    /// </summary>
    /// <param name="id">The ID of the movie.</param>
    /// <param name="imageId">The ID of the image to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The requested image details if found.</returns>
    [HttpGet(ApiEndpoints.MovieImages.GetImage)]
    [EnableRateLimiting(AuthConstants.DefaultPolicyName)]
    public async Task<ActionResult<ImageResponse>> GetImage(Guid id, Guid imageId, CancellationToken cancellationToken)
    {
        var image = await imageService.GetImageAsync(imageId, cancellationToken);
        
        if (image == null)
        {
            return NotFound();
        }

        if (image.MovieId != id)
        {
            return BadRequest("Image does not belong to the specified movie");
        }

        return Ok(image);
    }

    /// <summary>
    /// Sets an image as the primary image for a movie.
    /// </summary>
    /// <param name="id">The ID of the movie.</param>
    /// <param name="imageId">The ID of the image to be set as primary.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>No content if successful, otherwise an error response.</returns>
    [HttpPut(ApiEndpoints.MovieImages.SetPrimary)]
    public async Task<IActionResult> SetPrimaryImage(Guid id, Guid imageId, CancellationToken cancellationToken)
    {
        var image = await imageService.GetImageAsync(imageId, cancellationToken);
        
        if (image == null)
        {
            return NotFound();
        }

        if (image.MovieId != id)
        {
            return BadRequest("Image does not belong to the specified movie");
        }

        var result = await imageService.SetPrimaryImageAsync(imageId, cancellationToken);
        
        if (!result)
        {
            return StatusCode(500, "Failed to set primary image");
        }

        return NoContent();
    }
    
    /// <summary>
    /// Updates an existing image for a specific movie.
    /// </summary>
    /// <param name="id">The ID of the movie.</param>
    /// <param name="imageId">The ID of the image to update.</param>
    /// <param name="request">The updated image data.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The updated image details or an error response.</returns>
    [HttpPut(ApiEndpoints.MovieImages.Update)]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = 10_000_000)] // 10MB
    public async Task<IActionResult> UpdateImage(Guid id, Guid imageId, [FromForm] ImageUploadRequest request, CancellationToken cancellationToken)
    {
        var image = await imageService.GetImageAsync(imageId, cancellationToken);
        if (image == null)
        {
            return NotFound();
        }

        if (image.MovieId != id)
        {
            return BadRequest("Image does not belong to the specified movie");
        }

        try
        {
            var updatedImage = await imageService.UpdateImageAsync(imageId, request, cancellationToken);
            return Ok(updatedImage);
        }
        catch (ImageUploadException ex)
        {
            logger.LogWarning(ex, "Failed to update image for movie {MovieId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while updating image {ImageId} for movie {MovieId}", imageId, id);
            return StatusCode(500, "An error occurred while updating the image");
        }
    }
    
    /// <summary>
    /// Deletes a specific image from a movie.
    /// </summary>
    /// <param name="id">The ID of the movie.</param>
    /// <param name="imageId">The ID of the image to delete.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>No content if successful, otherwise an error response.</returns>
    [HttpDelete(ApiEndpoints.MovieImages.Delete)]
    public async Task<IActionResult> DeleteImage(Guid id, Guid imageId, CancellationToken cancellationToken)
    {
        var image = await imageService.GetImageAsync(imageId, cancellationToken);
        
        if (image == null)
        {
            return NotFound();
        }

        if (image.MovieId != id)
        {
            return BadRequest("Image does not belong to the specified movie");
        }

        var result = await imageService.DeleteImageAsync(imageId, cancellationToken);
        
        if (!result)
        {
            return StatusCode(500, "Failed to delete image");
        }

        return NoContent();
    }
    
    /// <summary>
    /// Deletes multiple images associated with a specific movie.
    /// </summary>
    /// <param name="id">The ID of the movie.</param>
    /// <param name="imageIds">A list of image IDs to delete.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>No content if successful, otherwise an error response.</returns>
    [HttpDelete(ApiEndpoints.MovieImages.BulkDelete)]
    public async Task<IActionResult> DeleteImages(Guid id, [FromBody] List<Guid>? imageIds, CancellationToken cancellationToken)
    {
        if (imageIds == null || imageIds.Count == 0)
        {
            return BadRequest("No image IDs provided");
        }

        // Optional: Validate images belong to the movie
        foreach (var imageId in imageIds)
        {
            var image = await imageService.GetImageAsync(imageId, cancellationToken);
            if (image is null)
                return NotFound($"Image {imageId} not found");

            if (image.MovieId != id)
                return BadRequest($"Image {imageId} does not belong to the specified movie");
        }

        var success = await bulkImageService.DeleteMultipleImagesAsync(imageIds, cancellationToken);
        return success ? NoContent() : StatusCode(500, "One or more images could not be deleted");
    }

}