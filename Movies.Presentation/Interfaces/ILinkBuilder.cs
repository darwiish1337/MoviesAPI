using Movies.Domain.Models;

namespace Movies.Presentation.Interfaces;

public interface ILinkBuilder
{
    List<Link> BuildForMovie(HttpContext httpContext, Movie movie);
    
    List<Link> BuildForPagination(HttpContext context, string endpointName, int page, int pageSize, int totalCount, object? extraQuery = null);
}
