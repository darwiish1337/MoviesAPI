using Microsoft.Extensions.DependencyInjection;
using Movies.Application.Abstractions.Persistence;
using Movies.Domain.Constants;
using Movies.Infrastructure.Configuration;
using Movies.Infrastructure.HealthChecks;
using Movies.Infrastructure.Interfaces.Services;
using Movies.Infrastructure.Persistence.Database;
using Movies.Infrastructure.Persistence.Repositories;
using Movies.Infrastructure.Services;

namespace Movies.Infrastructure.Extension;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<IMovieRepository, MovieRepository>();
        services.AddSingleton<IRatingRepository, RatingRepository>();
        services.AddSingleton<IMovieService, MovieService>();
        services.AddSingleton<IRatingService, RatingService>();

        return services;       
    }
    
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, DatabaseSettings settings)
    {
        services.AddSingleton<IDbConnectionFactory>(_ => 
            new NpgsqlDbConnectionFactory(settings.ConnectionString));

        services.AddSingleton<DbInitializer>();

        return services;
    }
    
    public static IServiceCollection AddCustomHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<CloudinaryHealthCheck>(ConfigurationKeys.Cloudinary)
            .AddCheck<RedisHealthCheck>(ConfigurationKeys.Redis);
        return services;
    }

}