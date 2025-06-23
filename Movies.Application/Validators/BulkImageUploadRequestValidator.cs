using FluentValidation;
using Movies.Application.DTOs.Requests;
using Movies.Application.Abstractions.Persistence;

namespace Movies.Application.Validators;

public class BulkImageUploadRequestValidator : AbstractValidator<BulkImageUploadRequest>
{
    public BulkImageUploadRequestValidator(IMovieRepository movieRepository)
    {
        RuleFor(x => x.MovieId)
            .NotEmpty()
            .MustAsync(async (id, ct) => await movieRepository.ExistsByIdAsync(id, ct))
            .WithMessage("Movie does not exist.");

        RuleFor(x => x.Files)
            .NotNull().WithMessage("Files are required.")
            .NotEmpty().WithMessage("At least one file is required.");

        RuleForEach(x => x.Files).SetValidator(new FileValidator());
    }
}