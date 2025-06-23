using Microsoft.AspNetCore.Http;

namespace Movies.Application.DTOs.Requests;

public class BulkImageUploadRequest
{
    public Guid MovieId { get; set; }
    
    public List<IFormFile> Files { get; set; } = [];
    
    public string? AltText { get; set; }
}