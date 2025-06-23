using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Movies.Application.Extension;
using Movies.Domain.Constants;
using Movies.Domain.Consts;
using Movies.Infrastructure.Configuration;
using Movies.Infrastructure.Extension;
using Movies.Infrastructure.Persistence.Database;
using Movies.Presentation.Extension;
using Movies.Presentation.Middlewares;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// Add services to the container

// Add authentication services
var jwtSettings = builder.Configuration.GetSection(ConfigurationKeys.Jwt).Get<JwtSettings>()!;

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

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthConstants.AdminUserPolicyName, p 
            => p.RequireClaim(AuthConstants.AdminUserClaimName,AuthClaimValues.True))
    .AddPolicy(AuthConstants.TrustedMemberPolicyName, p 
        => p.RequireAssertion(c 
            => c.User.HasClaim(m => m is { Type: AuthConstants.AdminUserClaimName, Value: AuthClaimValues.True})
            || c.User.HasClaim(m => m is { Type: AuthConstants.TrustedMemberClaimName, Value: AuthClaimValues.True})));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure DI for collection services
builder.Services.AddPresentationServices();
builder.Services.AddInfrastructureServices();
builder.Services.AddApplicationServices();
var dbSettings = config.GetSection(ConfigurationKeys.Database)
    .Get<DatabaseSettings>()!;
builder.Services.AddSingleton(dbSettings);
builder.Services.AddDatabaseServices(dbSettings);


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Add validation mapping middleware
app.UseMiddleware<ValidationMappingMiddleware>();

app.MapControllers();

var dbInitializer = app.Services.GetRequiredService<DbInitializer>();
    await dbInitializer.InitializeAsync();

app.Run();
