using Microsoft.AspNetCore.Http;

namespace Movies.Application.DTOs.Requests;

/// <summary>
/// Represents a request for uploading and processing an image associated with a movie.
/// This class encapsulates all necessary parameters for image upload, including file data,
/// movie association, image metadata, and processing options like resizing and format conversion.
/// </summary>
public class ImageUploadRequest
{
    /// <summary>
    /// The image file to upload.
    /// </summary>
    public required IFormFile File { get; init; }

    /// <summary>
    /// The ID of the movie the image is associated with.
    /// </summary>
    public required Guid MovieId { get; init; }

    /// <summary>
    /// Alternative text describing the image for accessibility and SEO.
    /// </summary>
    public string? AltText { get; init; } = string.Empty;

    /// <summary>
    /// Indicates whether this image is the primary image for the movie.
    /// </summary>
    // ReSharper disable once RedundantDefaultMemberInitializer
    public bool IsPrimary { get; init; } = false;

    /// <summary>
    /// Optional width (in pixels) to resize the image to.
    /// </summary>
    public int? Width { get; init; }

    /// <summary>
    /// Optional height (in pixels) to resize the image to.
    /// </summary>
    public int? Height { get; init; }

    /// <summary>
    /// The quality setting for the image (e.g., "auto", "80", etc.).
    /// </summary>
    public string? Quality { get; init; } = "auto";

    /// <summary>
    /// The desired output format of the image (e.g., "webp", "jpg").
    /// </summary>
    public string? Format { get; init; } = "webp";
}