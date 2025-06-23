using Movies.Domain.ValueObjects;

namespace Movies.Application.Abstractions.Services;

public interface ICloudinaryService
{
    Task<ImageUploadResult> UploadImageAsync(Stream imageStream, string fileName, string folder, CancellationToken cancellationToken = default);
    
    Task<DeletionResult> DeleteImageAsync(string publicId, CancellationToken cancellationToken = default);
    
    string GenerateUrl(string publicId, ImageTransformation? transformation = null);
    
    Task<GetResourceResult> GetImageDetailsAsync(string publicId, CancellationToken cancellationToken = default);
}