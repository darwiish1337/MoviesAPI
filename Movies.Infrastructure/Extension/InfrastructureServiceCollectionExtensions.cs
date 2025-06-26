using Microsoft.Extensions.DependencyInjection;
using Movies.Application.Abstractions.Persistence;
using Movies.Domain.Constants;
using Movies.Infrastructure.Configuration;
using Movies.Infrastructure.HealthChecks;
using Movies.Infrastructure.Persistence.Database;
using Movies.Infrastructure.Persistence.Repositories;

namespace Movies.Infrastructure.Extension;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<IMovieRepository, MovieRepository>();
        services.AddSingleton<IRatingRepository, RatingRepository>();

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
            .AddCheck<CloudinaryHealthCheck>(
                HealthCheckConstants.CloudinaryHealthCheck,
                tags: ["cloudinary", "storage"])

            .AddCheck<RedisHealthCheck>(
                HealthCheckConstants.RedisHealthCheck,
                tags: ["redis", "cache"])

            .AddCheck<DatabaseHealthCheck>(
                HealthCheckConstants.DatabaseHealthCheck,
                tags: ["db"])

            .AddCheck<MovieModuleHealthCheck>(
                HealthCheckConstants.MovieModuleHealthCheck,
                tags: ["movie"]);

        return services;
    }
}