using System.Xml.XPath;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Movies.Presentation.Swagger;

/// <summary>
/// Custom Swagger document filter to extract controller summaries from XML docs
/// and register them as tags with descriptions in the Swagger UI.
/// </summary>
public class SwaggerTagDescriptionsFilter : IDocumentFilter
{
    // Store's controller name and its summary description
    private readonly Dictionary<string, string> _controllerSummaries = new();

    public SwaggerTagDescriptionsFilter(IEnumerable<string> xmlPaths)
    {
        // Iterate over all XML documentation files
        foreach (var xmlPath in xmlPaths)
        {
            // Skip if a file does not exist
            if (!File.Exists(xmlPath)) continue;
            
            // Load the XML content
            var xml = new XPathDocument(xmlPath);
            var navigator = xml.CreateNavigator();
            
            // Select all <member> nodes that describe types (classes)
            var nodes = navigator.Select("/doc/members/member[starts-with(@name, 'T:')]");
            while (nodes.MoveNext())
            {
                // Get the full type name
                var name = nodes.Current?.GetAttribute("name", "");
                
                // Skip invalid or unwanted types (like SlugRegex or non-Controller classes)
                if (string.IsNullOrWhiteSpace(name) || name.Contains("SlugRegex") || !name.EndsWith("Controller"))
                    continue;
                
                // Get the <summary> tag content for the type
                var summaryNode = nodes.Current?.SelectSingleNode("summary");
                if (summaryNode is null) continue;
                
                // Extract just the controller name (remove namespace and "Controller" suffix)
                var controllerName = name
                    .Replace("T:", "")
                    .Split('.')
                    .Last()
                    .Replace("Controller", "");
                
                // Clean and store the summary text
                var summaryText = summaryNode.InnerXml.Trim();
                _controllerSummaries[controllerName] = summaryText;
            }
        }
    }

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // Loop through all extracted controller summaries
        foreach (var controllerName in _controllerSummaries.Keys)
        {
            // Avoid adding duplicate tags
            if (swaggerDoc.Tags.All(t => t.Name != controllerName))
            {
                // Add a new Swagger tag with description
                swaggerDoc.Tags.Add(new OpenApiTag
                {
                    Name = controllerName,
                    Description = _controllerSummaries[controllerName]
                });
            }
        }
    }
}