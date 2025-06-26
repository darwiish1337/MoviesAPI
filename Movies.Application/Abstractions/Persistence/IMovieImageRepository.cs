using Movies.Domain.Models;

namespace Movies.Application.Abstractions.Persistence;

public interface IMovieImageRepository
{
    Task<bool> CreateAsync(MovieImage image, CancellationToken cancellationToken = default);
    
    Task<MovieImage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<MovieImage>> GetByMovieIdAsync(Guid movieId, CancellationToken cancellationToken = default);
    
    Task<bool> UpdateAsync(MovieImage image, CancellationToken cancellationToken = default);
    
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<bool> SetPrimaryImageAsync(Guid imageId, Guid movieId, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<MovieImage>> GetAllAsync(CancellationToken cancellationToken = default);
    
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<MovieImage?> GetPrimaryImageAsync(Guid movieId, CancellationToken cancellationToken = default);

    Task<bool> CreateManyAsync(IEnumerable<MovieImage> images, CancellationToken cancellationToken = default);

    Task<bool> DeleteManyAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    Task<bool> UpdateManyAsync(IEnumerable<MovieImage> images, CancellationToken cancellationToken = default);
}