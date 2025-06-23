namespace Movies.Domain.ValueObjects;

public record ImageTransformation
{
    public int? Width { get; init; }
    public int? Height { get; init; }
    
    public string? Quality { get; init; } = "auto";
    
    public string? Format { get; init; } = "webp";
    
    public string? Crop { get; init; } = "fill";
    
    public string? Gravity { get; init; } = "face";
    
    public static ImageTransformation Thumbnail 
        => new() { Width = 300, Height = 450 };
    
    public static ImageTransformation Medium 
        => new() { Width = 600, Height = 900 };
    
    public static ImageTransformation Large 
        => new() { Width = 1200, Height = 1800 };
}
