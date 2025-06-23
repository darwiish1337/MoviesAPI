using Movies.Application.DTOs.Requests;
using Movies.Application.DTOs.Responses;

namespace Movies.Application.Abstractions.Services;

public interface IBulkImageService  
{
    Task<IEnumerable<ImageResponse>> UploadMultipleImagesAsync(IEnumerable<ImageUploadRequest> requests, CancellationToken cancellationToken = default);
    
    Task<bool> DeleteMultipleImagesAsync(IEnumerable<Guid>? imageIds, CancellationToken cancellationToken = default);
}