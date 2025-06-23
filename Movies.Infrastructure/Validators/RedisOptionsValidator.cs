using Microsoft.Extensions.Options;
using Movies.Infrastructure.Configuration;

namespace Movies.Infrastructure.Validators;

public class RedisOptionsValidator : IValidateOptions<RedisOptions>
{
    public ValidateOptionsResult Validate(string? name, RedisOptions options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
            errors.Add("ConnectionString is required");

        if (options.DefaultExpireTimeMinutes <= 0)
            errors.Add("DefaultExpireTimeMinutes must be greater than 0");

        return errors.Count > 0 
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}