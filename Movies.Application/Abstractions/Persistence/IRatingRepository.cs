using Movies.Domain.Models;

namespace Movies.Application.Abstractions.Persistence;

public interface IRatingRepository
{
    Task<bool> RateMovieAsync(Guid movieId, int rating, Guid userId, CancellationToken cancellationToken = default);

    Task<float?> GetRatingAsync(Guid movieId, CancellationToken cancellationToken = default);
    
    Task<(float? Rating, int? UserRating)> GetRatingAsync(Guid movieId, Guid userId, CancellationToken cancellationToken = default);

    Task<bool> DeleteRatingAsync(Guid movieId, Guid userId, CancellationToken cancellationToken = default);

    Task<IEnumerable<MovieRating>> GetRatingsForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}