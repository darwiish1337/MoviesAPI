using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Movies.Application.DTOs.Requests;
using Movies.Application.Mapping;
using Movies.Domain.Constants;
using Movies.Infrastructure.Interfaces.Services;
using Movies.Presentation.Auth;

namespace Movies.Presentation.Controllers;

[ApiController]
public class RatingsController(IRatingService ratingService) : ControllerBase
{
    [Authorize]
    [HttpPut(ApiEndpoints.Movies.Rate)]
    public async Task<IActionResult> RateMovie([FromRoute] Guid id, [FromBody] RateMovieRequest request, CancellationToken cancellationToken)
    {
        var userId = HttpContext.GetUserId();
        var result = await ratingService.RateMovieAsync(id, request.Rating, userId!.Value, cancellationToken);
        return result ? Ok() : NotFound();
    }
    
    [Authorize]
    [HttpDelete(ApiEndpoints.Movies.DeleteRating)]
    public async Task<IActionResult> DeleteRating([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var userId = HttpContext.GetUserId();
        var result = await ratingService.DeleteRatingAsync(id, userId!.Value, cancellationToken);
        return result ? Ok() : NotFound();       
    }

    [Authorize]
    [HttpGet(ApiEndpoints.Ratings.GetUsersRatings)]
    public async Task<IActionResult> GetUsersRatings(CancellationToken cancellationToken)
    {
        var userId = HttpContext.GetUserId();
        var ratings = await ratingService.GetRatingsForUserAsync(userId!.Value, cancellationToken);
        var ratingResponse = ratings.MapToResponse();
        
        return Ok(ratingResponse);       
    }
    
}