namespace Movies.Infrastructure.Configuration;

public class RedisOptions
{
    public const string SectionName = "Redis";
    
    public required string ConnectionString { get; set; }

    public int DefaultExpireTimeMinutes { get; set; } = 60;
    
    public string KeyPrefix { get; set; } = "MoviesApp";
}