namespace Movies.Infrastructure.Configuration;

public class JwtSettings
{
    public string Key { get; init; }  = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; }  = string.Empty;
}
