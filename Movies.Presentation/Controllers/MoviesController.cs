using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Movies.Application.DTOs.Requests;
using Movies.Application.Mapping;
using Movies.Domain.Consts;
using Movies.Infrastructure.Interfaces.Services;
using Movies.Presentation.Auth;
using Movies.Presentation.Interfaces;
using Movies.Presentation.Mapping;

namespace Movies.Presentation.Controllers;

[ApiController]
public class MoviesController(IMovieService movieService) : ControllerBase
{
    [Authorize(AuthConstants.TrustedMemberPolicyName)]
    [HttpPost(ApiEndpoints.Movies.Create)]
    public async Task<IActionResult> CreateMovie([FromBody]CreateMovieRequest request, CancellationToken cancellationToken)
    {
        var movie = request.MapToMovie();
        await movieService.CreateAsync(movie, cancellationToken);
        
        return CreatedAtAction(nameof(GetMovie), new { idOrSlug = movie.Id }, movie);
    }
    
    [Authorize(AuthConstants.TrustedMemberPolicyName)]
    [HttpPost(ApiEndpoints.Movies.BulkCreate)]
    public async Task<IActionResult> BulkCreateMovie([FromBody]List<CreateMovieRequest> requests, CancellationToken cancellationToken)
    {
        var movies = requests.Select(r => r.MapToMovie()).ToList();
        var results = await movieService.BulkCreateAsync(movies, cancellationToken);
        
        return Ok(results);
    }
    
    [HttpGet(ApiEndpoints.Movies.Get)]
    public async Task<IActionResult> GetMovie( [FromRoute] string idOrSlug, [FromServices] ILinkBuilder linkBuilder, CancellationToken cancellationToken)
    {
        var userId = HttpContext.GetUserId();
        var movie = Guid.TryParse(idOrSlug, out var id)
            ? await movieService.GetByIdAsync(id, userId, cancellationToken)
            : await movieService.GetBySlugAsync(idOrSlug, userId, cancellationToken);

        if (movie is null)
            return NotFound();

        var response = movie.MapToResponse();
        response.Links = linkBuilder.BuildForMovie(HttpContext, movie);

        return Ok(response);
    }
    
    [HttpGet(ApiEndpoints.Movies.GetAll)]
    public async Task<IActionResult> GetAllMovies([FromQuery] GetAllMoviesRequest request, [FromServices] ILinkBuilder linkBuilder,
        CancellationToken cancellationToken)
    {
        var userId  = HttpContext.GetUserId();
        var options = request.MapToOptions().WithUser(userId);
        var movies      = await movieService.GetAllAsync(options, cancellationToken);
        var totalCount  = await movieService.GetCountAsync(options.Title, options.YearOfRelease, cancellationToken);

        var response = movies.MapToResponseWithLinks(
            page:        request.Page,
            pageSize:    request.PageSize,
            totalCount:  totalCount,
            linkBuilder: linkBuilder,
            httpContext: HttpContext);
        
        response.Links = linkBuilder.BuildForPagination(
            HttpContext,
            endpointName: "GetAllMovies",
            page:        request.Page,
            pageSize:    request.PageSize,
            totalCount:  totalCount,
            extraQuery: new
            {
                request.Title,
                request.YearOfRelease,
                request.SortBy
            });

        return Ok(response);
    }
    
    [Authorize(AuthConstants.TrustedMemberPolicyName)]
    [HttpPut(ApiEndpoints.Movies.Update)]
    public async Task<IActionResult> UpdateMovie([FromRoute]Guid id, [FromBody]UpdateMovieRequest request, CancellationToken cancellationToken)
    {
        var movie = request.MapToMovie(id);
        var userId = HttpContext.GetUserId();
        var updatedMovie = await movieService.UpdateAsync(movie, userId, cancellationToken);
        if (updatedMovie is null)
        {
            return NotFound();
        }

        var response = updatedMovie.MapToResponse();
        
        return Ok(response);
    }

    [Authorize(AuthConstants.AdminUserPolicyName)]
    [HttpDelete(ApiEndpoints.Movies.Delete)]
    public async Task<IActionResult> DeleteMovie([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var deleted = await movieService.DeleteByIdAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        return Ok();
    }
    
    [Authorize(AuthConstants.AdminUserPolicyName)]
    [HttpDelete(ApiEndpoints.Movies.BulkDelete)]
    public async Task<IActionResult> BulkDeleteMovies([FromBody] BulkDeleteMoviesRequest request, CancellationToken cancellationToken)
    {
            var deleted = await movieService.DeleteBulkAsync(request, cancellationToken);
            if (!deleted)
            {
                return NotFound();
            }

            return Ok();
    }
     
}
