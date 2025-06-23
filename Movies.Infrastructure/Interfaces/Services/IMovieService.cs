using Movies.Application.DTOs.Requests;
using Movies.Application.DTOs.Responses;
using Movies.Domain.Models;

namespace Movies.Infrastructure.Interfaces.Services;

public interface IMovieService
{
    Task<bool> CreateAsync(Movie movie, CancellationToken cancellationToken = default);

    Task<List<MovieBulkCreationResponse>> BulkCreateAsync(IEnumerable<Movie> movies, CancellationToken cancellationToken = default);
    
    Task<Movie?> GetByIdAsync(Guid id, Guid? userid = null, CancellationToken cancellationToken = default);
    
    Task<Movie?> GetBySlugAsync(string slug, Guid? userid = null, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<Movie>> GetAllAsync(GetAllMoviesOptions options, CancellationToken cancellationToken = default);
    
    Task<Movie?> UpdateAsync(Movie movie, Guid? userid = null, CancellationToken cancellationToken = default);
    
    Task<bool> DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> DeleteBulkAsync(BulkDeleteMoviesRequest request, CancellationToken ct = default);

    Task<int> GetCountAsync(string? title, int? yearOfRelease, CancellationToken cancellationToken = default);
}