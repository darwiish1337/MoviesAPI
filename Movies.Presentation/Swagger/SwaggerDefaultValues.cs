using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Movies.Presentation.Swagger;

public class SwaggerDefaultValues : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var apiDescription = context.ApiDescription;

        operation.Deprecated |= apiDescription.IsDeprecated();

        foreach (var responseType in apiDescription.SupportedResponseTypes)
        {
            var responseKey = responseType.IsDefaultResponse
                ? "default"
                : responseType.StatusCode.ToString();

            if (!operation.Responses.TryGetValue(responseKey, out var response))
                continue;

            foreach (var contentType in response.Content.Keys.ToList())
            {
                if (responseType.ApiResponseFormats.All(x => x.MediaType != contentType))
                {
                    response.Content.Remove(contentType);
                }
            }
        }

        if (operation.Parameters == null)
            return;

        foreach (var parameter in operation.Parameters)
        {
            var description = apiDescription.ParameterDescriptions
                .FirstOrDefault(p => p.Name == parameter.Name);

            if (description == null)
                continue;

            parameter.Description ??= description.ModelMetadata?.Description;

            if (parameter.Schema.Default == null 
                && description is { DefaultValue: not null, ModelMetadata.ModelType: not null })
            {
                try
                {
                    var json = JsonSerializer.Serialize(
                        description.DefaultValue,
                        description.ModelMetadata.ModelType);

                    parameter.Schema.Default = OpenApiAnyFactory.CreateFromJson(json);
                }
                catch
                {
                    // تجاهل الأخطاء لو serialization فشل
                }
            }

            parameter.Required |= description.ModelMetadata?.IsRequired ?? false;
        }
    }
}