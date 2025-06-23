using FluentValidation;
using Microsoft.Extensions.DependencyInjection;


namespace Movies.Application.Extension;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<IApplicationMarker>(ServiceLifetime.Singleton);
        
        return services;    
    }
}
