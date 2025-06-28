namespace Movies.Domain.ValueObjects;

public class ImageUploadError
{
    public string Message { get; init; } = string.Empty;
    public int HttpCode { get; set; }
}
