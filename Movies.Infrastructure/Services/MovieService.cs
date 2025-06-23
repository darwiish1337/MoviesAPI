using FluentValidation;
using Movies.Application.Abstractions.Persistence;
using Movies.Application.DTOs.Requests;
using Movies.Application.DTOs.Responses;
using Movies.Domain.Models;
using Movies.Infrastructure.Interfaces.Services;

namespace Movies.Infrastructure.Services;

public class MovieService(IMovieRepository movieRepository, IValidator<Movie> movieValidator, IRatingRepository ratingRepository, 
    IValidator<List<Guid>> bulkDeleteValidator,
    IValidator<GetAllMoviesOptions> optionsValidator) : IMovieService
{
    public  async Task<bool> CreateAsync(Movie movie, CancellationToken cancellationToken = default)
    {
        await movieValidator.ValidateAndThrowAsync(movie, cancellationToken: cancellationToken);
        return await movieRepository.CreateAsync(movie, cancellationToken);
    }
    
    public async Task<List<MovieBulkCreationResponse>> BulkCreateAsync( IEnumerable<Movie> movies, CancellationToken ct = default)
    {
        var results = new List<MovieBulkCreationResponse>();

        foreach (var movie in movies)
        {
            try
            {
                await movieValidator.ValidateAndThrowAsync(movie, ct);

                var success = await movieRepository.CreateBulkAsync(movie, ct);

                results.Add(new MovieBulkCreationResponse
                {
                    MovieId  = movie.Id,
                    Title    = movie.Title,
                    YearOfRelease = movie.YearOfRelease,
                    Slug     = movie.Slug,
                    Genres   = movie.Genres.ToList()
                });
            }
            catch (ValidationException ex)
            {
                results.Add(new MovieBulkCreationResponse
                {
                    MovieId  = movie.Id,
                    Title    = movie.Title,
                    YearOfRelease = movie.YearOfRelease,
                    Slug     = movie.Slug,
                    Genres   = movie.Genres.ToList(),
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
    
    public async Task<Movie?> GetByIdAsync(Guid id, Guid? userid = null, CancellationToken cancellationToken = default)
    {
        return await movieRepository.GetByIdAsync(id, userid, cancellationToken);
    }

    public async Task<Movie?> GetBySlugAsync(string slug, Guid? userid = null, CancellationToken cancellationToken = default)
    {
        return await movieRepository.GetBySlugAsync(slug, userid, cancellationToken);
    }

    public async Task<IEnumerable<Movie>> GetAllAsync(GetAllMoviesOptions options, CancellationToken cancellationToken = default)
    {
        await optionsValidator.ValidateAndThrowAsync(options, cancellationToken: cancellationToken);
        return await movieRepository.GetAllAsync(options, cancellationToken);
    }

    public async Task<Movie?> UpdateAsync(Movie movie, Guid? userid = null, CancellationToken cancellationToken = default)
    {
        await movieValidator.ValidateAndThrowAsync(movie, cancellationToken: cancellationToken);
        var movieExists = await movieRepository.ExistsByIdAsync(movie.Id, cancellationToken);
        if (!movieExists)
        {
            return null;
        }

        await movieRepository.UpdateAsync(movie, cancellationToken);

        if (!userid.HasValue)
        {
            var rating = await ratingRepository.GetRatingAsync(movie.Id, cancellationToken);
            movie.Rating = rating;
            return movie;
        }
        
        var ratings = await ratingRepository.GetRatingAsync(movie.Id, userid.Value, cancellationToken);
        movie.Rating = ratings.Rating;
        movie.UserRating = ratings.UserRating;
        return movie;
    }

    public async Task<bool> DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await movieRepository.DeleteByIdAsync(id, cancellationToken);
    }
    
    public async Task<bool> DeleteBulkAsync(BulkDeleteMoviesRequest request, CancellationToken ct = default)
    {
        var validationResult = await bulkDeleteValidator.ValidateAsync(request.MovieIds, ct);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        return await movieRepository.DeleteBulkAsync(request.MovieIds, ct);
    }

    public async Task<int> GetCountAsync(string? title, int? yearOfRelease, CancellationToken cancellationToken = default)
    {
        return await movieRepository.GetCountAsync(title, yearOfRelease, cancellationToken);
    }
}
