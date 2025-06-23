using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Movies.Application.Abstractions.Services;
using Movies.Application.Services;

namespace Movies.Application.Extension;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IBulkImageService, BulkImageService>();
        
        services.AddValidatorsFromAssemblyContaining<IApplicationMarker>(ServiceLifetime.Singleton);
        
        return services;    
    }
}
