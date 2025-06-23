using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
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
var jwtSettings = config.GetSection(ConfigurationKeys.Jwt).Get<JwtSettings>()!;

builder.Services.AddAuthentication(options =>
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

// -----------------------------
// Authorization Policies
// -----------------------------
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthConstants.AdminUserPolicyName, p =>
        p.RequireClaim(AuthConstants.AdminUserClaimName, AuthClaimValues.True))
    .AddPolicy(AuthConstants.TrustedMemberPolicyName, p =>
        p.RequireAssertion(c =>
            c.User.HasClaim(m => m is { Type: AuthConstants.AdminUserClaimName, Value: AuthClaimValues.True }) ||
            c.User.HasClaim(m => m is { Type: AuthConstants.TrustedMemberClaimName, Value: AuthClaimValues.True })));

// -----------------------------
// API Versioning
// -----------------------------
builder.Services.AddApiVersioning(x =>
{
    x.DefaultApiVersion = new ApiVersion(1.0);
    x.AssumeDefaultVersionWhenUnspecified = true;
    x.ReportApiVersions = true;
    x.ApiVersionReader = new MediaTypeApiVersionReader("api-version");
}).AddMvc().AddApiExplorer();

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
builder.Services.AddValidation();

// -----------------------------
// Background Services
// -----------------------------
builder.Services.AddHostedService<ImageCleanupService>();

var app = builder.Build();

// -----------------------------
// Map Health Checks Endpoint
// -----------------------------
app.MapHealthChecks("/_health");

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