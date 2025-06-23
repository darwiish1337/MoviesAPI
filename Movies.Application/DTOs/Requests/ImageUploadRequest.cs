using Microsoft.AspNetCore.Http;

namespace Movies.Application.DTOs.Requests;

public class ImageUploadRequest
{
    public required IFormFile File { get; init; }
    
    public required Guid MovieId { get; init; }
    
    public string? AltText { get; init; } = string.Empty;
    
    public bool IsPrimary { get; init; } = false;
    
    // Transformation options
    public int? Width { get; init; }
    
    public int? Height { get; init; }
    
    public string? Quality { get; init; } = "auto";
    
    public string? Format { get; init; } = "webp";
}