namespace Movies.Domain.ValueObjects;

public class ImageUploadResult
{
    public string PublicId { get; init; } = string.Empty;
    public string SecureUrl { get; set; } = string.Empty;
    public int Width { get; init; }
    public int Height { get; init; }
    public long Bytes { get; init; }
    public string Format { get; init; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public ImageUploadError? Error { get; init; }
    public DateTime CreatedAt { get; set; }
}

