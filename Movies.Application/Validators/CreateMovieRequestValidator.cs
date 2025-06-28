using FluentValidation;
using Movies.Application.DTOs.Requests;
using Movies.Application.Helpers;

namespace Movies.Application.Validators;

public class CreateMovieRequestValidator : AbstractValidator<CreateMovieRequest>
{
    public CreateMovieRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.")
            .Must(SanitizationHelper.IsSafeText)
            .WithMessage("Title contains unsafe or disallowed characters.");

        RuleFor(x => x.YearOfRelease)
            .InclusiveBetween(1900, DateTime.UtcNow.Year + 1)
            .WithMessage($"Year of release must be between 1900 and {DateTime.UtcNow.Year + 1}.");

        RuleFor(x => x.Genres)
            .NotEmpty().WithMessage("At least one genre is required.")
            .Must(genres => genres.All(g => !string.IsNullOrWhiteSpace(g)))
            .WithMessage("Genres cannot contain empty or whitespace-only values.");

        RuleForEach(x => x.Genres)
            .Must(SanitizationHelper.IsSafeText)
            .WithMessage("One or more genres contain unsafe or disallowed characters.");
    }
}