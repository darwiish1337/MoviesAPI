using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using Movies.Application.Abstractions.Services;
using Movies.Domain.ValueObjects;
using Movies.Infrastructure.Configuration;

namespace Movies.Infrastructure.Services;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;
    private readonly CloudinaryOptions _options;

    public CloudinaryService(IOptions<CloudinaryOptions> options)
    {
        _options = options.Value;
        var account = new Account(_options.CloudName, _options.ApiKey, _options.ApiSecret);
        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = _options.UseSecureUrls;
    }

    public async Task<Domain.ValueObjects.ImageUploadResult> UploadImageAsync(Stream imageStream, string fileName, string folder, 
        CancellationToken cancellationToken = default)
    {
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, imageStream),
            Folder = folder,
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = false,
            Transformation = new Transformation()
                .Quality("auto")
                .FetchFormat("auto")
        };

        var cloudinaryResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

        // Map Cloudinary result to our domain object
        return new Domain.ValueObjects.ImageUploadResult
        {
            PublicId = cloudinaryResult.PublicId ?? string.Empty,
            SecureUrl = cloudinaryResult.SecureUrl?.ToString() ?? string.Empty,
            Width = cloudinaryResult.Width,
            Height = cloudinaryResult.Height,
            Bytes = cloudinaryResult.Bytes,
            Format = cloudinaryResult.Format ?? string.Empty,
            Version = cloudinaryResult.Version ?? string.Empty,
            CreatedAt = cloudinaryResult.CreatedAt,
            Error = cloudinaryResult.Error != null ? new ImageUploadError
            {
                Message = cloudinaryResult.Error.Message ?? string.Empty,
                HttpCode = (int)cloudinaryResult.StatusCode
            } : null
        };
    }

    public async Task<Domain.ValueObjects.DeletionResult> DeleteImageAsync(string publicId, CancellationToken cancellationToken = default)
    {
        var deleteParams = new DeletionParams(publicId)
        {
            ResourceType = ResourceType.Image
        };

        var cloudinaryResult = await _cloudinary.DestroyAsync(deleteParams);

        return new Domain.ValueObjects.DeletionResult
        {
            Result = cloudinaryResult.Result,
            Error = cloudinaryResult.Error?.Message
        };
    }

    public string GenerateUrl(string publicId, ImageTransformation? transformation = null)
    {
        if (transformation == null)
        {
            return _cloudinary.Api.UrlImgUp.BuildUrl(publicId);
        }

        var cloudinaryTransformation = new Transformation();

        if (transformation.Width.HasValue)
            cloudinaryTransformation = cloudinaryTransformation.Width(transformation.Width.Value);

        if (transformation.Height.HasValue)
            cloudinaryTransformation = cloudinaryTransformation.Height(transformation.Height.Value);

        if (!string.IsNullOrEmpty(transformation.Quality))
            cloudinaryTransformation = cloudinaryTransformation.Quality(transformation.Quality);

        if (!string.IsNullOrEmpty(transformation.Format))
            cloudinaryTransformation = cloudinaryTransformation.FetchFormat(transformation.Format);

        if (!string.IsNullOrEmpty(transformation.Crop))
            cloudinaryTransformation = cloudinaryTransformation.Crop(transformation.Crop);

        if (!string.IsNullOrEmpty(transformation.Gravity))
            cloudinaryTransformation = cloudinaryTransformation.Gravity(transformation.Gravity);

        return _cloudinary.Api.UrlImgUp.Transform(cloudinaryTransformation).BuildUrl(publicId);
    }

    public async Task<Domain.ValueObjects.GetResourceResult> GetImageDetailsAsync(string publicId, CancellationToken cancellationToken = default)
    {
        var cloudinaryResult = await _cloudinary.GetResourceAsync(publicId, cancellationToken);

        return new Domain.ValueObjects.GetResourceResult
        {
            PublicId = cloudinaryResult.PublicId ?? string.Empty,
            Width = cloudinaryResult.Width,
            Height = cloudinaryResult.Height,
            Bytes = cloudinaryResult.Bytes,
            Format = cloudinaryResult.Format ?? string.Empty,
            SecureUrl = cloudinaryResult.SecureUrl ?? string.Empty,
            CreatedAt = DateTime.TryParse(cloudinaryResult.CreatedAt, out var createdAt) ? createdAt : default,
            Metadata = cloudinaryResult.ImageMetadata?.ToDictionary(x => x.Key, x => (object)x.Value) ?? new Dictionary<string, object>()
        };
    }
}