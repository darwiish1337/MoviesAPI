namespace Movies.Domain.Constants;

public static class ApiEndpoints
{
    private const string ApiBase = "api";

    public static class Movies
    {
        private const string Base = $"{ApiBase}/movies";

        public const string Create = Base;
        public const string BulkCreate = $"{Base}/bulk";
        public const string Get = $"{Base}/{{idOrSlug}}";
        public const string GetAll = Base;
        public const string Update = $"{Base}/{{id:guid}}";
        public const string Delete = $"{Base}/{{id:guid}}";
        public const string BulkDelete = $"{Base}/bulk";
        
        public const string Rate = $"{Base}/{{id:guid}}/ratings";
        public const string DeleteRating = $"{Base}/{{id:guid}}/ratings";
    }
    
    public static class  Ratings
    {
        private const string Base = $"{ApiBase}/movies";
        
        public const string GetUsersRatings = $"{Base}/me";
    }
    
    public static class MovieImages
    {
        private const string Base = $"{ApiBase}/movies";
        
        public const string Create = $"{Base}/{{id:guid}}/images";
        
        public const string BulkCreate = $"{Base}/{{id:guid}}/images/bulk";
        
        public const string GetMovieImages = $"{Base}/{{id:guid}}/images";
        
        public const string GetImage = $"{Base}/{{id:guid}}/{{imageId:guid}}/images";
        
        public const string SetPrimary = $"{Base}/{{id:guid}}/{{imageId}}/primary";
        
        public const string Update = $"{Base}/{{id:guid}}/images/{{imageId}}";
        
        public const string BulkUpdate = Base + "/{id:guid}/images/bulk";
        
        public const string Delete = $"{Base}/{{id:guid}}/images/{{imageId}}";
        
        public const string BulkDelete = $"{Base}/{{id:guid}}/images/bulk";

        //public const string BulkDelete = $"{Base}/{{id:guid}}/images/{{imageId}}/bulk";
    }
}
