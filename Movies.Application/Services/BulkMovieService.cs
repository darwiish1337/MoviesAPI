using FluentValidation;
using Movies.Application.Abstractions.Persistence;
using Movies.Application.Abstractions.Services;
using Movies.Application.DTOs.Requests;
using Movies.Application.DTOs.Responses;
using Movies.Domain.Models;

namespace Movies.Application.Services;

public class BulkMovieService(IMovieRepository movieRepository, IValidator<Movie> movieValidator,
    IValidator<List<Guid>> bulkDeleteValidator) : IBulkMovieService
{
    public async Task<List<MovieBulkCreationResponse>> BulkCreateAsync(IEnumerable<Movie> movies, CancellationToken ct = default)
    {
        var results = new List<MovieBulkCreationResponse>();

        foreach (var movie in movies)
        {
            try
            {
                await movieValidator.ValidateAndThrowAsync(movie, ct);

                await movieRepository.CreateBulkAsync(movie, ct);

                results.Add(new MovieBulkCreationResponse
                {
                    MovieId = movie.Id,
                    Title = movie.Title,
                    YearOfRelease = movie.YearOfRelease,
                    Slug = movie.Slug,
                    Genres = movie.Genres.ToList()
                });
            }
            catch (ValidationException ex)
            {
                results.Add(new MovieBulkCreationResponse
                {
                    MovieId = movie.Id,
                    Title = movie.Title,
                    YearOfRelease = movie.YearOfRelease,
                    Slug = movie.Slug,
                    Genres = movie.Genres.ToList(),
                    Errors = ex.Errors.Select(e => new ValidationResponse
                    {
                        PropertyName = e.PropertyName,
                        Message = e.ErrorMessage
                    }).ToList()
                });
            }
        }

        return results;
    }

    public async Task<bool> DeleteBulkAsync(BulkDeleteMoviesRequest request, CancellationToken ct = default)
    {
        var validationResult = await bulkDeleteValidator.ValidateAsync(request.MovieIds, ct);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        return await movieRepository.DeleteBulkAsync(request.MovieIds, ct);
    }
}
