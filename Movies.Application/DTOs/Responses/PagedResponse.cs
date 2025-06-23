namespace Movies.Application.DTOs.Responses;

public class PagedResponse<TResponse> : HalResponse
{
    public required IEnumerable<TResponse> Movies { get; init; } = [];
    
    public required int PageSize { get; init; }
    
    public required int Page { get; init; }
    
    public required int TotalCount { get; init; }
    
    public bool HasNextPage => TotalCount > (Page * PageSize);
    
    public bool HasPreviousPage => Page > 1;
    
}