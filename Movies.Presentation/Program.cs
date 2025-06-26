using Movies.Application.Extension;
using Movies.Domain.Constants;
using Movies.Infrastructure.BackgroundServices;
using Movies.Infrastructure.Configuration;
using Movies.Infrastructure.Extension;
using Movies.Infrastructure.Persistence.Database;
using Movies.Presentation.Extension;
using Movies.Presentation.Middlewares;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// -----------------------------
// Authentication Configuration
// -----------------------------
builder.Services.AddJwtAuthentication(config);

// -----------------------------
// Authorization Policies
// -----------------------------
builder.Services.AddAuthorizationPolicies();

// -----------------------------
// API Versioning
// -----------------------------
builder.Services.AddApiVersioningSupport();
builder.Services.AddEndpointsApiExplorer();

// -----------------------------
// Controllers
// -----------------------------
builder.Services.AddControllers();

// -----------------------------
// Swagger Configuration
// -----------------------------
builder.Services.AddSwaggerDocumentation();

// -----------------------------
// Health Checks
// -----------------------------
builder.Services.AddCustomHealthChecks();

// -----------------------------
// Service Registration via Extension Methods
// -----------------------------
builder.Services.AddPresentationServices();     // Presentation layer (middlewares, filters)
builder.Services.AddInfrastructureServices();   // Infrastructure dependencies (Cloudinary, Redis, etc)
builder.Services.AddApplicationServices();      // Application layer services and validators

// -----------------------------
// Database Configuration
// -----------------------------
var dbSettings = config.GetSection(ConfigurationKeys.Database).Get<DatabaseSettings>()!;
builder.Services.AddSingleton(dbSettings);
builder.Services.AddDatabaseServices(dbSettings);

// -----------------------------
// Rate Limiting
// -----------------------------
builder.Services.AddRateLimiting();

// -----------------------------
// Image Management
// -----------------------------
builder.Services.AddImageManagement(config);

// -----------------------------
// Validation (FluentValidation)
// -----------------------------
builder.Services.AddValidationLayer();
builder.Services.AddValidation();

// -----------------------------
// Background Services
// -----------------------------
builder.Services.AddHostedService<ImageCleanupService>();

var app = builder.Build();

// -----------------------------
// Map Health Checks Endpoint
// -----------------------------
app.MapDynamicHealthCheck();

// -----------------------------
// Development Tools (Swagger)
// -----------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(x =>
    {
        foreach (var description in app.DescribeApiVersions())
        {
            x.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName);
        }
    });
}

// -----------------------------
// Middleware Pipeline
// -----------------------------
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ValidationMappingMiddleware>(); // Maps FluentValidation errors
app.UseMiddleware<ImageUploadExceptionMiddleware>(); // Global image exception handler

// -----------------------------
// Controllers Routing
// -----------------------------
app.MapControllers();

// -----------------------------
// Database Initialization
// -----------------------------
var dbInitializer = app.Services.GetRequiredService<DbInitializer>();
await dbInitializer.InitializeAsync();

app.Run();