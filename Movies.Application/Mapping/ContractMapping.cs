using Movies.Application.DTOs.Requests;
using Movies.Application.DTOs.Responses;
using Movies.Domain.Enums;
using Movies.Domain.Models;

namespace Movies.Application.Mapping;

public static class ContractMapping
{
    public static Movie MapToMovie(this CreateMovieRequest request)
    {
        return new Movie
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            YearOfRelease = request.YearOfRelease,
            Genres = request.Genres.ToList()
        };
    }

    public static Movie MapToMovie(this UpdateMovieRequest request, Guid existingId)
    {
        return new Movie
        {
            Id = existingId,
            Title = request.Title,
            YearOfRelease = request.YearOfRelease,
            Genres = request.Genres.ToList()
        };
    }

    public static MovieResponse MapToResponse(this Movie movie)
    {
        return new MovieResponse
        {
            Id = movie.Id,
            Title = movie.Title,
            Slug = movie.Slug,
            Rating = movie.Rating,
            UserRating = movie.UserRating,
            YearOfRelease = movie.YearOfRelease,
            Genres = movie.Genres
        };
    }

    public static MoviesResponse MapToResponse(this IEnumerable<Movie> movies, int page, int pageSize, int totalCount)
    {
        return new MoviesResponse
        {
            Movies = movies.Select(MapToResponse),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount       
        };
    }

    public static IEnumerable<MovieRatingResponse> MapToResponse(this IEnumerable<MovieRating> ratings)
    {
        return ratings.Select(x => new MovieRatingResponse
        {
            Rating = x.Rating,
            Slug = x.Slug,
            MovieId = x.MovieId
        });
    }

    public static GetAllMoviesOptions MapToOptions(this GetAllMoviesRequest request)
    {
        var rawSort = request.SortBy?.Trim();
        var field   = rawSort?.TrimStart('+', '-');

        return new GetAllMoviesOptions
        {
            Title         = request.Title,
            YearOfRelease = request.YearOfRelease,
            SortField     = field,
            SortOrder     = rawSort is null
                ? SortOrder.Unsorted
                : rawSort.StartsWith('-')
                    ? SortOrder.Descending
                    : SortOrder.Ascending,
            Page      = request.Page     <= 0 ? 1  : request.Page,
            PageSize  = request.PageSize <= 0 ? 10 : request.PageSize
        };
    }

    public static GetAllMoviesOptions WithUser(this GetAllMoviesOptions options, Guid? userId)
    {
        options.UserId = userId;
        return options;         
    }
}