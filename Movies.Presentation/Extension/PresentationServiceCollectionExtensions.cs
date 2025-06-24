using System.Text;
using Microsoft.Extensions.Options;
using Movies.Application.Abstractions.Caching;
using Movies.Application.Abstractions.Persistence;
using Movies.Application.Abstractions.Services;
using Movies.Infrastructure.Caching;
using Movies.Infrastructure.Configuration;
using Movies.Infrastructure.Persistence.Repositories;
using Movies.Infrastructure.Services;
using Movies.Infrastructure.Validators;
using Movies.Presentation.Services;
using StackExchange.Redis;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Movies.Domain.Constants;
using Movies.Presentation.Swagger;
using ILinkBuilder = Movies.Presentation.Interfaces.ILinkBuilder;

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
        services.AddSingleton<ICloudinaryService, CloudinaryService>();
        services.AddSingleton<ICacheService, RedisCacheService>();
        services.AddSingleton<IImageService, ImageService>();
        services.AddSingleton<IMovieImageRepository, MovieImageRepository>();

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
        services.AddSwaggerGen(options =>
        {
            options.OperationFilter<SwaggerDefaultValues>();

            var xmlFileNames = new[]
            {
                "Movies.Presentation.xml",
                "Movies.Application.xml",
                "Movies.Domain.xml",
                "Movies.Infrastructure.xml"
            };

            var availableXmlFiles = xmlFileNames
                .Select(file => Path.Combine(AppContext.BaseDirectory, file))
                .Where(File.Exists)
                .ToList();

            foreach (var path in availableXmlFiles)
            {
                options.IncludeXmlComments(path);
            }

            // Register SwaggerTagDescriptionsFilter with paths
            options.DocumentFilter<SwaggerTagDescriptionsFilter>(new object[] { availableXmlFiles });
        });

        return services;
    }
    
    public static IServiceCollection AddApiVersioningSupport(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new MediaTypeApiVersionReader("api-version");
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        return services;
    }
    
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection(ConfigurationKeys.Jwt).Get<JwtSettings>()!;

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true
                };
            });

        return services;
    }

    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(AuthConstants.AdminUserPolicyName, p =>
                p.RequireClaim(AuthConstants.AdminUserClaimName, AuthClaimValues.True))
            .AddPolicy(AuthConstants.TrustedMemberPolicyName, p =>
                p.RequireAssertion(c =>
                    c.User.HasClaim(m => m is { Type: AuthConstants.AdminUserClaimName, Value: AuthClaimValues.True }) ||
                    c.User.HasClaim(m => m is { Type: AuthConstants.TrustedMemberClaimName, Value: AuthClaimValues.True })));

        return services;
    }

}