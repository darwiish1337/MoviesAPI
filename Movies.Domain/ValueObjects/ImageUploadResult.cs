namespace Movies.Domain.ValueObjects;

public class ImageUploadResult
{
    public string PublicId { get; set; } = string.Empty;
    public string SecureUrl { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public long Bytes { get; set; }
    public string Format { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public ImageUploadError? Error { get; set; }
    public DateTime CreatedAt { get; set; }
}

