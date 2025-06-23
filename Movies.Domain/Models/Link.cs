using System.Text.Json.Serialization;
using Movies.Domain.Enums;

namespace Movies.Domain.Models;

public class Link
{
    public required string? Href { get; init; }
    
    public required string Rel { get; init; }
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required LinkType Type { get; init; }
}