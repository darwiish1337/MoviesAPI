using System.Text.RegularExpressions;

namespace Movies.Domain.Models;

public partial class Movie
{
    [GeneratedRegex(@"[^\p{L}\p{N} _-]", RegexOptions.NonBacktracking)]
    private static partial Regex SlugRegex();
    
    private string _title = string.Empty;
    
    public required Guid Id { get; init; }
    
    public required string Title
    {
        get => _title;
        set => _title = CleanTitle(value);
    }
    
    public string Slug => GenerateSlug();

    public required int YearOfRelease { get; set; }
    
    public float? Rating { get; set; }
    
    public int? UserRating { get; set; }

    public required List<string> Genres { get; init; } = [];
    
    public List<MovieImage> Images { get; init; } = [];
    public MovieImage? PrimaryImage
        => Images.FirstOrDefault(x => x.IsPrimary);
    

    #region Private Methods
    private string GenerateSlug()
    {
        var sluggedTitle = SlugRegex()
            .Replace(Title, "")             
            .Trim()                         
            .Replace(" ", "-")              
            .ToLowerInvariant();           

        return $"{sluggedTitle}-{YearOfRelease}";
    }
    
    private static string CleanTitle(string rawTitle)
    {
        if (string.IsNullOrWhiteSpace(rawTitle))
            return string.Empty;
        
        var cleaned = Regex.Replace(rawTitle, @"[^\p{L}\p{N}\s]", "");
        
        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
        
        cleaned = Regex.Replace(cleaned, @"\d{5,}", "");

        return cleaned;
    }
    #endregion
    
}
