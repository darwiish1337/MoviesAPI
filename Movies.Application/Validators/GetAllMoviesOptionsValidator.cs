using FluentValidation;
using Movies.Domain.Constants;
using Movies.Domain.Models;

namespace Movies.Application.Validators;

public class GetAllMoviesOptionsValidator : AbstractValidator<GetAllMoviesOptions>
{
    public GetAllMoviesOptionsValidator()
    {
        RuleFor(x => x.YearOfRelease)
            .LessThanOrEqualTo(DateTime.UtcNow.Year);

        RuleFor(x => x.SortField)
            .Must(x => string.IsNullOrWhiteSpace(x) ||
                       MovieConstants.AcceptableSortFields.Contains(x.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage("You can only sort by title or year.");
        
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);
        
        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 25)
            .WithMessage("Page size must be between 1 and 25 movies per page.");
        
        
        
        
    }
}