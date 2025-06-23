using Microsoft.Extensions.Options;
using Movies.Application.Abstractions.Caching;
using Movies.Application.Abstractions.Persistence;
using Movies.Application.Abstractions.Services;
using Movies.Application.Services;
using Movies.Infrastructure.Caching;
using Movies.Infrastructure.Configuration;
using Movies.Infrastructure.Persistence.Repositories;
using Movies.Infrastructure.Services;
using Movies.Infrastructure.Validators;
using Movies.Presentation.Interfaces;
using Movies.Presentation.Services;
using StackExchange.Redis;
using System.Threading.RateLimiting;
using Movies.Presentation.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Movies.Presentation.Extension;

public static class PresentationServiceCollectionExtensions
{
    public static IServiceCollection AddPresentationServices(this IServiceCollection services)
    {
        services.AddSingleton<ILinkBuilder, LinkBuilder>();
        
        return services;
    }
    
    public static IServiceCollection AddImageManagement(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure options
        services.Configure<CloudinaryOptions>(configuration.GetSection(CloudinaryOptions.SectionName));
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));

        // Add Redis
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var redisOptions = provider.GetRequiredService<IOptions<RedisOptions>>().Value;
            return ConnectionMultiplexer.Connect(redisOptions.ConnectionString);
        });

        // Register services
        services.AddScoped<ICloudinaryService, CloudinaryService>();
        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<IImageService, ImageService>();
        services.AddScoped<IMovieImageRepository, MovieImageRepository>();

        return services;
    }

    public static IServiceCollection AddValidation(this IServiceCollection services)
    {
        services.AddTransient<IValidateOptions<CloudinaryOptions>, CloudinaryOptionsValidator>();
        services.AddTransient<IValidateOptions<RedisOptions>, RedisOptionsValidator>();

        return services;
    }

    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Default policy for general API usage
            options.AddPolicy("DefaultPolicy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            // Stricter policy for image uploads
            options.AddPolicy("ImageUploadPolicy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1)
                    }));
        });

        return services;
    }
    
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        services.AddSwaggerGen(x => x.OperationFilter<SwaggerDefaultValues>());
        return services;
    }
}