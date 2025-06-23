using Movies.Domain.Enums;
using Movies.Domain.Models;
using Movies.Presentation.Interfaces;

namespace Movies.Presentation.Services;

public class LinkBuilder(LinkGenerator linkGen) : ILinkBuilder
{
    public List<Link> BuildForMovie(HttpContext ctx, Movie movie)
    {
        var id = movie.Id;

        return
        [
            Create(ctx, "GetMovie", "self", LinkType.GET, new { idOrSlug = id }),
            Create(ctx, "UpdateMovie", "update", LinkType.PUT, new { id }),
            Create(ctx, "DeleteMovie", "delete", LinkType.DELETE, new { id })
        ];
    }

    public List<Link> BuildForPagination(HttpContext context, string endpointName, int page, int pageSize, int totalCount, object? extraQuery = null)
    {
        var links = new List<Link>();

        var values = new Dictionary<string, object?>
        {
            ["pageSize"] = pageSize
        };

        if (extraQuery is not null)
        {
            foreach (var prop in extraQuery.GetType().GetProperties())
                values[prop.Name] = prop.GetValue(extraQuery);
        }

        var lastPage = (int)Math.Ceiling((double)totalCount / pageSize);

        links.Add(Create(context, endpointName, "self", LinkType.GET, WithPage(values, page)));
        links.Add(Create(context, endpointName, "first", LinkType.GET, WithPage(values, 1)));
        links.Add(Create(context, endpointName, "last", LinkType.GET, WithPage(values, lastPage)));

        if (page > 1)
            links.Add(Create(context, endpointName, "prev", LinkType.GET, WithPage(values, page - 1)));

        if (page * pageSize < totalCount)
            links.Add(Create(context, endpointName, "next", LinkType.GET, WithPage(values, page + 1)));

        return links;
    }

    private static Dictionary<string, object?> WithPage(Dictionary<string, object?> baseValues, int page) =>
        new(baseValues) { ["page"] = page };
    private Link Create(HttpContext ctx, string action, string rel, LinkType type, object values) =>
        new()
        {
            Href = linkGen.GetPathByAction(ctx, action, controller:"Movies", values),
            Rel  = rel,
            Type = type
        };
}