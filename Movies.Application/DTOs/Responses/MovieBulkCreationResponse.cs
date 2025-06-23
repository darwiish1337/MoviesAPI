namespace Movies.Application.DTOs.Responses;

public class MovieBulkCreationResponse
{
    public required Guid MovieId { get; init; }
    
    public required string Title { get; init; }
    
    public required int YearOfRelease { get; init; }
    
    public required string Slug { get; init; }
    
    public required IEnumerable<string> Genres { get; init; } = [];
    
    public IEnumerable<ValidationResponse>? Errors { get; init; }
}
