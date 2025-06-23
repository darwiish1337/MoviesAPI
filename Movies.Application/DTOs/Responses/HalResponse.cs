using Movies.Domain.Models;
using System.Text.Json.Serialization;

namespace Movies.Application.DTOs.Responses;

public abstract class HalResponse
{
    [JsonPropertyName("_links")] 
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<Link> Links { get; set; } = [];
}