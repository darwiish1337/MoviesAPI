namespace Movies.Domain.ValueObjects;

public class ImageUploadError
{
    public string Message { get; set; } = string.Empty;
    public int HttpCode { get; set; }
}
