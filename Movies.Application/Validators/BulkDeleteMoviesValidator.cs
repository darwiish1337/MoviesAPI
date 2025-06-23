using FluentValidation;
using Movies.Application.Abstractions.Persistence;

namespace Movies.Application.Validators;

public class BulkDeleteMoviesValidator : AbstractValidator<List<Guid>>
{
    public BulkDeleteMoviesValidator(IMovieRepository movieRepository)
    {
        RuleFor(x => x)
            .NotEmpty()
            .WithMessage("MovieIds list cannot be empty.");

        RuleForEach(x => x)
            .MustAsync(async (id, ct) => await movieRepository.ExistsByIdAsync(id, ct))
            .WithMessage((list, id) => $"Movie with ID '{id}' does not exist.");

    }
}