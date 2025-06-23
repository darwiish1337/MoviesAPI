using Movies.Presentation.Interfaces;
using Movies.Presentation.Services;

namespace Movies.Presentation.Extension;

public static class PresentationServiceCollectionExtensions
{
    public static IServiceCollection AddPresentationServices(this IServiceCollection services)
    {
        services.AddSingleton<ILinkBuilder, LinkBuilder>();
        
        return services;
    }
}