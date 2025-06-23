using FluentValidation;
using Microsoft.AspNetCore.Http;
using Movies.Domain.Constants;

namespace Movies.Application.Validators;

public class FileValidator : AbstractValidator<IFormFile>
{
    public FileValidator()
    {
        RuleFor(file => file.Length)
            .GreaterThan(0).WithMessage("File is empty.")
            .LessThanOrEqualTo(ImageConstants.MaxFileSize)
            .WithMessage($"File too large (max {ImageConstants.MaxFileSize / (1024 * 1024)}MB)");

        RuleFor(file => Path.GetExtension(file.FileName).ToLowerInvariant())
            .Must(ext => ImageConstants.AllowedExtensions.Contains(ext))
            .WithMessage(file => $"Unsupported file type {Path.GetExtension(file.FileName)}");

        RuleFor(file => file.ContentType.ToLowerInvariant())
            .Must(contentType => new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" }.Contains(contentType))
            .WithMessage(file => $"Unsupported content type {file.ContentType}");
        
        RuleFor(file => file.ContentType)
            .Must(contentType =>
                new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" }
                    .Contains(contentType.ToLowerInvariant()))
            .WithMessage(file => $"Invalid content type: {file.ContentType}");
    }
}