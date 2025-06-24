using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Movies.Application.Abstractions.Services;
using Movies.Application.Services;

namespace Movies.Application.Extension;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<IBulkMovieService, BulkMovieService>();
        services.AddSingleton<IMovieService, MovieService>();
        services.AddSingleton<IRatingService, RatingService>();
        
        return services;    
    }

    public static IServiceCollection AddValidationLayer(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<IApplicationMarker>(ServiceLifetime.Singleton);

        return services;   
    }
}
