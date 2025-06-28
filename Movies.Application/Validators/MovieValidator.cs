using FluentValidation;
using Movies.Application.Abstractions.Persistence;
using Movies.Application.Helpers;
using Movies.Domain.Models;


namespace Movies.Application.Validators;

public class MovieValidator : AbstractValidator<Movie> 
{
    private readonly IMovieRepository _movieRepository;
    
    public MovieValidator(IMovieRepository movieRepository)
    {
        _movieRepository = movieRepository;
        
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Genres)
            .NotEmpty().WithMessage("At least one genre is required.")
            .Must(list => list.Count is >= 2 and <= 100).WithMessage("Genres must be between 2 and 100 items.")
            .ForEach(genre =>
            {
                genre
                    .NotEmpty().WithMessage("Genre cannot be empty.")
                    .Length(2, 30).WithMessage("Each genre must be between 2 and 30 characters.");
            });

        RuleFor(x => x.Title)
            .Length(2, 100).WithMessage("Title must be between 2 and 100 characters.")
            .Must(title => !SanitizationHelper.ContainsDangerousContent(title)).WithMessage("Title contains invalid characters.")
            .Custom((title, context) =>
            {
                var cleaned = SanitizationHelper.SanitizeString(title);
                if (string.IsNullOrWhiteSpace(cleaned))
                    context.AddFailure("Title cannot be empty or whitespace.");
            });

        RuleFor(x => x.YearOfRelease)
            .LessThanOrEqualTo(DateTime.UtcNow.Year);
        
        RuleFor(x => x)
            .CustomAsync(CheckIfMovieExistsAsync);

        RuleFor(x => x.Slug)
            .MustAsync(ValidateSlug)
            .WithMessage("Slug already exists.");
}

    #region Private Methods

    private async Task<bool> ValidateSlug(Movie movie, string slug, CancellationToken cancellationToken = default)
    {
        var existingMovie = await _movieRepository.GetBySlugAsync(slug, cancellationToken: cancellationToken);
        
        if (existingMovie is not null)
            return existingMovie.Id == movie.Id;
        
        return existingMovie is null;       
    }
    
    private async Task CheckIfMovieExistsAsync(Movie movie, ValidationContext<Movie> ctx, CancellationToken cancellationToken)
    {
        var exists = await _movieRepository.ExistsAsync(movie.Title, movie.YearOfRelease, cancellationToken);
        if (!exists) return;

        ctx.AddFailure(nameof(Movie.Title),
            $"Title '{movie.Title}' already exists for year {movie.YearOfRelease}.");

        ctx.AddFailure(nameof(Movie.YearOfRelease),
            $"Another movie titled '{movie.Title}' already exists for year {movie.YearOfRelease}.");
    }

    #endregion
    
}