using Movies.Application.DTOs.Requests;
using Movies.Application.DTOs.Responses;
using Movies.Domain.ValueObjects;

namespace Movies.Application.Abstractions.Services;

public interface IImageService
{
    Task<ImageResponse> UploadImageAsync(ImageUploadRequest request, CancellationToken cancellationToken = default);
    
    Task<bool> DeleteImageAsync(Guid imageId, CancellationToken cancellationToken = default);
    
    Task<ImageResponse?> GetImageAsync(Guid imageId, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<ImageResponse>> GetMovieImagesAsync(Guid movieId, CancellationToken cancellationToken = default);
    
    Task<bool> SetPrimaryImageAsync(Guid imageId, CancellationToken cancellationToken = default);
    
    Task<ImageResponse> UpdateImageAsync(Guid imageId, ImageUploadRequest request, CancellationToken cancellationToken = default);
    
    string GenerateTransformedUrlAsync(string publicId, ImageTransformation transformation);
}