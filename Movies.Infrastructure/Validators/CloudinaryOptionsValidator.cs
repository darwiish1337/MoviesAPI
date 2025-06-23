using Microsoft.Extensions.Options;
using Movies.Infrastructure.Configuration;

namespace Movies.Infrastructure.Validators;

public class CloudinaryOptionsValidator : IValidateOptions<CloudinaryOptions>
{
    public ValidateOptionsResult Validate(string? name, CloudinaryOptions options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.CloudName))
            errors.Add("CloudName is required");

        if (string.IsNullOrWhiteSpace(options.ApiKey))
            errors.Add("ApiKey is required");

        if (string.IsNullOrWhiteSpace(options.ApiSecret))
            errors.Add("ApiSecret is required");

        if (options.UploadTimeout <= 0)
            errors.Add("UploadTimeout must be greater than 0");

        return errors.Count > 0 
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}