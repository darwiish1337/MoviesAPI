using Movies.Domain.Models;

namespace Movies.Application.Abstractions.Persistence;

public interface IMovieRepository
{
    Task<bool> CreateAsync(Movie movie, CancellationToken cancellationToken = default);

    Task<bool> CreateBulkAsync(Movie movie, CancellationToken cancellationToken = default);
    
    Task<Movie?> GetByIdAsync(Guid id, Guid? userid = null, CancellationToken cancellationToken = default);
    
    Task<Movie?> GetBySlugAsync(string slug, Guid? userid = null, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<Movie>> GetAllAsync(GetAllMoviesOptions options, CancellationToken cancellationToken = default);
    
    Task<bool> UpdateAsync(Movie movie, CancellationToken cancellationToken = default);
    
    Task<bool> DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> DeleteBulkAsync(List<Guid> ids, CancellationToken ct = default);

    Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<bool> ExistsAsync(string title, int yearOfRelease, CancellationToken cancellationToken = default);
    
    Task<int> GetCountAsync(string? title, int? yearOfRelease, CancellationToken cancellationToken = default);
}