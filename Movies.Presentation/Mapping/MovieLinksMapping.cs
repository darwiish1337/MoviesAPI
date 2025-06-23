using Movies.Application.DTOs.Responses;
using Movies.Application.Mapping;  
using Movies.Domain.Models;
using Movies.Presentation.Interfaces;

namespace Movies.Presentation.Mapping;

public static class MovieLinksMapping
{
    public static MoviesResponse MapToResponseWithLinks(this IEnumerable<Movie> movies, int page, int pageSize, int totalCount, ILinkBuilder linkBuilder,
        HttpContext httpContext)
    {
        var movieResponses = movies.Select(movie =>
        {
            var dto = movie.MapToResponse();
            dto.Links = linkBuilder.BuildForMovie(httpContext, movie);
            return dto;
        });

        return new MoviesResponse
        {
            Movies = movieResponses,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
