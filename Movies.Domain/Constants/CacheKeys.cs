namespace Movies.Domain.Constants;

public static class CacheKeys
{
    // Get All Movies
    public const string MoviesCache = "MoviesCache";
    public const string MoviesTag = "movies";
    public static readonly string[] MovieFilters = ["title", "year", "sortBy", "page", "pageSize"];
    
    // Get Movie By IdOrSlug
    public const string MovieCache = "MovieCache";
    public const string MovieTag = "movie";
    
    //
    
}