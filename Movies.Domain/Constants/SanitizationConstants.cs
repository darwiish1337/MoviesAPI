namespace Movies.Domain.Constants;

public static class SanitizationConstants
{
    public static readonly string[] SafeQueryKeys =
    [
        "page",
        "pageSize",
        "title",
        "sortBy",
        "year",
        "genres",
        "userId",
        "movieId",
        "tag",
        "imageId"
    ];
}