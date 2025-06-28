using Movies.Domain.Enums;

namespace Movies.Domain.Models;

public class GetAllMoviesOptions
{
    public string? Title { get; init; }
    
    public int? YearOfRelease { get; init; }
    
    public Guid? UserId { get; set; }
    
    public string? SortField { get; init; }
    
    public SortOrder? SortOrder { get; init; }

    public int? Page { get; init; } 

    public int? PageSize { get; init; } 
}