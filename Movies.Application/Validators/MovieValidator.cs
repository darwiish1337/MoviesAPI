using FluentValidation;
using Movies.Application.Abstractions.Persistence;
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
            .NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty();

        RuleFor(x => x.YearOfRelease)
            .LessThanOrEqualTo(DateTime.UtcNow.Year);
        
        RuleFor(x => x)
            .CustomAsync(CheckIfMovieExistsAsync);

        RuleFor(x => x.Slug)
            .MustAsync(ValidateSlug)
            .WithMessage("Slug already exists.");
}

    private async Task<bool> ValidateSlug(Movie movie, string slug, CancellationToken cancellationToken = default)
    {
        var existingMovie = await _movieRepository.GetBySlugAsync(slug, cancellationToken: cancellationToken);
        
        if (existingMovie is not null)
            return existingMovie.Id == movie.Id;
        
        return existingMovie is null;       
    }
    
    private async Task CheckIfMovieExistsAsync(
        Movie movie,
        ValidationContext<Movie> ctx,
        CancellationToken ct)
    {
        var exists = await _movieRepository.ExistsAsync(movie.Title, movie.YearOfRelease, ct);
        if (!exists) return;

        ctx.AddFailure(nameof(Movie.Title),
            $"Title '{movie.Title}' already exists for year {movie.YearOfRelease}.");

        ctx.AddFailure(nameof(Movie.YearOfRelease),
            $"Another movie titled '{movie.Title}' already exists for year {movie.YearOfRelease}.");
    }
    
}