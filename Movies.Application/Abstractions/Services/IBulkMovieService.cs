using Movies.Application.DTOs.Requests;
using Movies.Application.DTOs.Responses;
using Movies.Domain.Models;

namespace Movies.Application.Abstractions.Services;

public interface IBulkMovieService
{
    Task<List<MovieBulkCreationResponse>> BulkCreateAsync(IEnumerable<Movie> movies, CancellationToken cancellationToken = default);
    
    Task<bool> DeleteBulkAsync(BulkDeleteMoviesRequest request, CancellationToken ct = default);
}