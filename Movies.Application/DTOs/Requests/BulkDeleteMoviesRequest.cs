namespace Movies.Application.DTOs.Requests;

public class BulkDeleteMoviesRequest
{
    public List<Guid> MovieIds { get; init; } = [];
}
