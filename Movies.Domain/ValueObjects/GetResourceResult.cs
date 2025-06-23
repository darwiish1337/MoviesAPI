namespace Movies.Domain.ValueObjects;

public class GetResourceResult
{
    public string PublicId { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public long Bytes { get; set; }
    public string Format { get; set; } = string.Empty;
    public string SecureUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}