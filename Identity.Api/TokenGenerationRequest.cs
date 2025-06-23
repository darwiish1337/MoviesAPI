namespace Identity.Api;

public class TokenGenerationRequest
{
    public Guid UserId { get; set; }
    
    public required string Email { get; set; }

    public Dictionary<string, object> CustomClaims { get; set; } = new();
}