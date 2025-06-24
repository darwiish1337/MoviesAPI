using FluentValidation;
using Movies.Application.Abstractions.Persistence;
using Movies.Application.Abstractions.Services;
using Movies.Domain.Models;

namespace Movies.Application.Services;

public class MovieService(IMovieRepository movieRepository, IValidator<Movie> movieValidator, IRatingRepository ratingRepository, 
     IValidator<GetAllMoviesOptions> optionsValidator) : IMovieService
{
    public  async Task<bool> CreateAsync(Movie movie, CancellationToken cancellationToken = default)
    {
        await movieValidator.ValidateAndThrowAsync(movie, cancellationToken: cancellationToken);
        return await movieRepository.CreateAsync(movie, cancellationToken);
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
    
    public async Task<int> GetCountAsync(string? title, int? yearOfRelease, CancellationToken cancellationToken = default)
    {
        return await movieRepository.GetCountAsync(title, yearOfRelease, cancellationToken);
    }
}
