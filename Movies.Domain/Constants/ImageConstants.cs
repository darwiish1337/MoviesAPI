namespace Movies.Domain.Constants;

public static class ImageConstants
{
    public static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    
    public const long MaxFileSize = 10 * 1024 * 1024; // 10MB
    
    public const string CacheKeyPrefix = "movie_image";
}