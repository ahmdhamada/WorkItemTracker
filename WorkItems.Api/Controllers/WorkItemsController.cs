using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkItems.Api.Contracts;
using WorkItems.Api.Data;
using WorkItems.Api.Models;
namespace WorkItems.Api.Controllers;
[ApiController, Route("api/work-items")]
public sealed class WorkItemsController(WorkItemsDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<WorkItemResponse>> Create(CreateWorkItemRequest request, CancellationToken ct)
    {
        var title = request.Title.Trim();
        if (title.Length == 0) return BadRequest(new ProblemDetails { Title = "Title is required." });
        var item = new WorkItem { Title = title, Description = request.Description, CreatedAt = DateTimeOffset.UtcNow };
        db.WorkItems.Add(item); await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, ToResponse(item));
    }
    [HttpGet]
    public async Task<ActionResult<PagedResponse<WorkItemResponse>>> List([FromQuery] string? title, [FromQuery] WorkItemStatus? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100) return BadRequest(new ProblemDetails { Title = "Page must be >= 1 and pageSize between 1 and 100." });
        var query = db.WorkItems.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(title)) query = query.Where(x => x.Title.Contains(title.Trim()));
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        var count = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return Ok(new PagedResponse<WorkItemResponse>(items.Select(ToResponse).ToList(), page, pageSize, count));
    }
    [HttpGet("{id:int}")]
    public async Task<ActionResult<WorkItemResponse>> GetById(int id, CancellationToken ct)
    { var item = await db.WorkItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct); return item is null ? NotFound() : Ok(ToResponse(item)); }
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateStatusRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<WorkItemStatus>(request.Status, true, out var next) || !Enum.IsDefined(next)) return BadRequest(new ProblemDetails { Title = "Status must be Todo, InProgress, or Done." });
        var item = await db.WorkItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound();
        if ((int)next != (int)item.Status + 1) return Conflict(new ProblemDetails { Title = $"Cannot change status from {item.Status} to {next}." });
        item.Status = next; await db.SaveChangesAsync(ct); return Ok(ToResponse(item));
    }
    private static WorkItemResponse ToResponse(WorkItem x) => new(x.Id, x.Title, x.Description, x.Status, x.CreatedAt);
}
