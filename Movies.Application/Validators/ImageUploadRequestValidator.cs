using FluentValidation;
using Movies.Application.DTOs.Requests;
using Movies.Domain.Constants;

namespace Movies.Application.Validators;

public class ImageUploadRequestValidator : AbstractValidator<ImageUploadRequest>
{
    public ImageUploadRequestValidator()
    {
        RuleFor(x => x.File)
            .NotNull().WithMessage("File is required.")
            .Must(file => file.Length > 0).WithMessage("File is empty.")
            .Must(file => file.Length <= ImageConstants.MaxFileSize)
            .WithMessage($"File size exceeds max {ImageConstants.MaxFileSize / (1024 * 1024)}MB.")
            .Must(file =>
            {
                if (file == null) return false;

                var allowedExt = ImageConstants.AllowedExtensions;
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                return allowedExt.Contains(ext);
            }).WithMessage(_ =>
                $"Unsupported file extension. Allowed: {string.Join(", ", ImageConstants.AllowedExtensions)}")
            .Must(file =>
            {
                if (file == null) return false;

                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
                return allowedTypes.Contains(file.ContentType.ToLowerInvariant());
            }).WithMessage(_ => $"Invalid content type.");

            RuleFor(x => x.MovieId)
                .NotEmpty().WithMessage("MovieId is required.");
    }
}
