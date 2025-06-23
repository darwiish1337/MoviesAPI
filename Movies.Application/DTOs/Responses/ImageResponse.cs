namespace Movies.Application.DTOs.Responses;

public class ImageResponse
{
    public required Guid Id { get; init; }
    
    public required Guid MovieId { get; init; }
    
    public required string OriginalUrl { get; init; }
    
    public required string ThumbnailUrl { get; init; }
    
    public required string MediumUrl { get; init; }
    
    public required string LargeUrl { get; init; }
    
    public required string? AltText { get; init; }
    
    public required int Width { get; init; }
    
    public required int Height { get; init; }
    
    public required long Size { get; init; }
    
    public required string Format { get; init; }
    
    public required bool IsPrimary { get; init; }
    
    public required DateTime CreatedAt { get; init; }
}