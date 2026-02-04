using Microsoft.AspNetCore.Mvc;

namespace SuperAdminDashboard.API.Controllers;

/// <summary>
/// Base API controller with common functionality
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Returns a success response with data
    /// </summary>
    protected IActionResult Success<T>(T data, string? message = null)
    {
        return Ok(new
        {
            success = true,
            data,
            message
        });
    }

    /// <summary>
    /// Returns a success response with paginated data
    /// </summary>
    protected IActionResult SuccessPaginated<T>(
        IReadOnlyList<T> items,
        int page,
        int pageSize,
        int totalCount,
        string? message = null)
    {
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        
        return Ok(new
        {
            success = true,
            data = items,
            pagination = new
            {
                page,
                pageSize,
                totalCount,
                totalPages,
                hasNext = page < totalPages,
                hasPrevious = page > 1
            },
            message
        });
    }

    /// <summary>
    /// Returns a created response
    /// </summary>
    protected IActionResult Created<T>(T data, string? location = null)
    {
        return StatusCode(201, new
        {
            success = true,
            data,
            message = "Created successfully"
        });
    }

    /// <summary>
    /// Returns a no content response
    /// </summary>
    protected new IActionResult NoContent()
    {
        return StatusCode(204);
    }
}
