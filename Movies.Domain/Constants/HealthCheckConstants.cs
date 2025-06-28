namespace Movies.Domain.Constants;

public abstract class HealthCheckConstants
{
    public const string CloudinaryHealthCheck = "Cloudinary Storage";
    
    public const string RedisHealthCheck = "Redis Cache";
    
    public const string DatabaseHealthCheck = "PostgreSQL Database";

    public const string MovieModuleHealthCheck = "Movie Module";
    
    public const string HealthCheckEndpoint = "/_health";
}