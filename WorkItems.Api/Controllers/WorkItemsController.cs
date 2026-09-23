using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using WorkItems.Api.Contracts;
using WorkItems.Api.Models;
using WorkItems.Api.Services;

namespace WorkItems.Api.Controllers;

[ApiController, Route("api/work-items")]
public sealed class WorkItemsController(IWorkItemsService workItems) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<WorkItemResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WorkItemResponse>> Create(CreateWorkItemRequest request, CancellationToken cancellationToken)
    {
        var result = await workItems.CreateAsync(request.Title, request.Description, cancellationToken);
        if (!result.Succeeded)
            return BadRequest(new ProblemDetails { Title = "Title must contain at least one non-whitespace character." });

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpGet]
    [ProducesResponseType<PagedResponse<WorkItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<WorkItemResponse>>> List(
        [FromQuery] string? title,
        [FromQuery] WorkItemStatus? status,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await workItems.ListAsync(title, status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<WorkItemResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkItemResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await workItems.GetByIdAsync(id, cancellationToken);
        return result.Succeeded ? Ok(result.Value) : NotFound();
    }

    [HttpPatch("{id:int}/status")]
    [ProducesResponseType<WorkItemResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkItemResponse>> UpdateStatus(int id, UpdateStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await workItems.ChangeStatusAsync(id, request.Status, cancellationToken);
        return result.Error switch
        {
            WorkItemError.None => Ok(result.Value),
            WorkItemError.InvalidStatus => BadRequest(new ProblemDetails { Title = "Status must be Todo, InProgress, or Done." }),
            WorkItemError.NotFound => NotFound(),
            WorkItemError.InvalidTransition => Conflict(new ProblemDetails { Title = "The requested status transition is not allowed." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
