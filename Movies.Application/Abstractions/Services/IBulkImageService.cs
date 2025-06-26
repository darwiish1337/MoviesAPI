using Movies.Application.DTOs.Requests;
using Movies.Application.DTOs.Responses;
using Movies.Domain.Models;

namespace Movies.Application.Abstractions.Services;

public interface IBulkImageService  
{
    Task<IEnumerable<ImageResponse>> UploadImagesAsync(IEnumerable<ImageUploadRequest> requests, CancellationToken cancellationToken = default);
    
    Task<bool> DeleteImagesAsync(IEnumerable<Guid>? imageIds, CancellationToken cancellationToken = default);

    Task<bool> UpdateImagesAsync(IEnumerable<MovieImage> images, CancellationToken cancellationToken = default);
}