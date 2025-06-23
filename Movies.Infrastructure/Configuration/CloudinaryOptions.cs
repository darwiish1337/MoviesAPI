namespace Movies.Infrastructure.Configuration;

public class CloudinaryOptions
{
    public const string SectionName = "Cloudinary";
    
    public required string CloudName { get; set; }
    
    public required string ApiKey { get; set; }
    
    public required string ApiSecret { get; set; }

    public bool UseSecureUrls { get; set; } = true;
    
    public string DefaultFolder { get; set; } = "movies";
    
    public int UploadTimeout { get; set; } = 60; // seconds
}
