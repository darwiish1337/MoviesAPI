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
// Configuration Binding
// -----------------------------
var corsSettings = config.GetSection(CorsSettings.SectionName).Get<CorsSettings>()!;
var dbSettings = config.GetSection(ConfigurationKeys.Database).Get<DatabaseSettings>()!;
builder.Services.Configure<CorsSettings>(config.GetSection(CorsSettings.SectionName));
builder.Services.AddSingleton(dbSettings);

// -----------------------------
// CORS
// -----------------------------
builder.Services.AddCorsPolicies(corsSettings);

// -----------------------------
// Authentication / Authorization
// -----------------------------
builder.Services.AddJwtAuthentication(config);
builder.Services.AddAuthorizationPolicies();

// -----------------------------
// Controllers & API Versioning
// -----------------------------
builder.Services.AddControllers();
builder.Services.AddApiVersioningSupport();
builder.Services.AddEndpointsApiExplorer();

// -----------------------------
// Swagger
// -----------------------------
builder.Services.AddSwaggerDocumentation();

// -----------------------------
// Output Caching
// -----------------------------
builder.Services.AddCustomOutputCache();

// -----------------------------
// Validation (FluentValidation)
// -----------------------------
builder.Services.AddValidationLayer();
builder.Services.AddValidation();

// -----------------------------
// Rate Limiting
// -----------------------------
builder.Services.AddRateLimiting();

// -----------------------------
// Database
// -----------------------------
builder.Services.AddDatabaseServices(dbSettings);

// -----------------------------
// Image Management
// -----------------------------
builder.Services.AddImageManagement(config);

// -----------------------------
// Health Checks
// -----------------------------
builder.Services.AddCustomHealthChecks();

// -----------------------------
// Application / Infrastructure / Presentation Layers
// -----------------------------
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();
builder.Services.AddPresentationServices();

// -----------------------------
// Background Services
// -----------------------------
builder.Services.AddHostedService<ImageCleanupService>();

// -----------------------------
// Build App
// -----------------------------
var app = builder.Build();

// -----------------------------
// Development Tools (Swagger)
// -----------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        foreach (var description in app.DescribeApiVersions())
        {
            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName);
        }
    });
}

// -----------------------------
// Middleware Pipeline
// -----------------------------
app.UseHttpsRedirection();
app.UseCors(ConfigurationKeys.Cors);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();

app.UseMiddleware<RateLimitingResponseMiddleware>();
app.UseMiddleware<ValidationMappingMiddleware>();
app.UseMiddleware<ImageUploadExceptionMiddleware>();
app.UseMiddleware<CspMiddleware>();
app.UseMiddleware<InputSanitizationMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

// -----------------------------
// Endpoints
// -----------------------------
app.MapControllers();
app.MapDynamicHealthCheck();

// -----------------------------
// DB Initialization
// -----------------------------
var dbInitializer = app.Services.GetRequiredService<DbInitializer>();
await dbInitializer.InitializeAsync();

app.Run();
