using Movies.Domain.Models;

namespace Movies.Infrastructure.Interfaces.Services;

public interface IRatingService
{
    Task<bool> RateMovieAsync(Guid movieId, int rating, Guid userId, CancellationToken cancellationToken = default);

    Task<bool> DeleteRatingAsync(Guid movieId, Guid userId, CancellationToken cancellationToken = default);

    Task<IEnumerable<MovieRating>> GetRatingsForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}