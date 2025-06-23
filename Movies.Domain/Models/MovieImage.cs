namespace Movies.Domain.Models;

public  class MovieImage
{
    public required Guid Id { get; init; }
    
    public required Guid MovieId { get; init; }
    
    // Cloudinary public ID
    public required string PublicId { get; init; } 
    
    public required string OriginalUrl { get; init; }
    
    public required string ThumbnailUrl { get; init; }
    
    public required string MediumUrl { get; init; }
    
    public required string LargeUrl { get; init; }
    
    public required string? AltText { get; set; }
    
    public required int Width { get; init; }
    
    public required int Height { get; init; }
    
    public required long Size { get; init; } // in bytes
    
    public required string Format { get; init; } 
    
    public required bool IsPrimary { get; set; }
    
    public required DateTime CreatedAt { get; init; }
    
    public DateTime? UpdatedAt { get; set; }
}