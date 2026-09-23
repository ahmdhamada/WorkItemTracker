using Microsoft.EntityFrameworkCore;
using WorkItems.Api.Contracts;
using WorkItems.Api.Data;
using WorkItems.Api.Models;

namespace WorkItems.Api.Services;

public enum WorkItemError { None, InvalidTitle, InvalidStatus, NotFound, InvalidTransition }

public sealed record WorkItemResult<T>(T? Value, WorkItemError Error = WorkItemError.None)
{
    public bool Succeeded => Error == WorkItemError.None;
    public static WorkItemResult<T> Success(T value) => new(value);
    public static WorkItemResult<T> Failure(WorkItemError error) => new(default, error);
}

public interface IWorkItemsService
{
    Task<WorkItemResult<WorkItemResponse>> CreateAsync(string title, string? description, CancellationToken cancellationToken);
    Task<PagedResponse<WorkItemResponse>> ListAsync(string? title, WorkItemStatus? status, int page, int pageSize, CancellationToken cancellationToken);
    Task<WorkItemResult<WorkItemResponse>> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<WorkItemResult<WorkItemResponse>> ChangeStatusAsync(int id, string status, CancellationToken cancellationToken);
}

public sealed class WorkItemsService(WorkItemsDbContext db) : IWorkItemsService
{
    public async Task<WorkItemResult<WorkItemResponse>> CreateAsync(string title, string? description, CancellationToken cancellationToken)
    {
        var normalizedTitle = title.Trim();
        if (normalizedTitle.Length == 0) return WorkItemResult<WorkItemResponse>.Failure(WorkItemError.InvalidTitle);

        var item = new WorkItem
        {
            Title = normalizedTitle,
            Description = description,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.WorkItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return WorkItemResult<WorkItemResponse>.Success(ToResponse(item));
    }

    public async Task<PagedResponse<WorkItemResponse>> ListAsync(string? title, WorkItemStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = db.WorkItems.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(title)) query = query.Where(item => item.Title.Contains(title.Trim()));
        if (status.HasValue) query = query.Where(item => item.Status == status.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<WorkItemResponse>(items.Select(ToResponse).ToList(), page, pageSize, totalCount);
    }

    public async Task<WorkItemResult<WorkItemResponse>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var item = await db.WorkItems.AsNoTracking().FirstOrDefaultAsync(workItem => workItem.Id == id, cancellationToken);
        return item is null
            ? WorkItemResult<WorkItemResponse>.Failure(WorkItemError.NotFound)
            : WorkItemResult<WorkItemResponse>.Success(ToResponse(item));
    }

    public async Task<WorkItemResult<WorkItemResponse>> ChangeStatusAsync(int id, string status, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<WorkItemStatus>(status, ignoreCase: true, out var nextStatus) || !Enum.IsDefined(nextStatus))
            return WorkItemResult<WorkItemResponse>.Failure(WorkItemError.InvalidStatus);

        var item = await db.WorkItems.FirstOrDefaultAsync(workItem => workItem.Id == id, cancellationToken);
        if (item is null) return WorkItemResult<WorkItemResponse>.Failure(WorkItemError.NotFound);
        if (!item.TryTransitionTo(nextStatus)) return WorkItemResult<WorkItemResponse>.Failure(WorkItemError.InvalidTransition);

        await db.SaveChangesAsync(cancellationToken);
        return WorkItemResult<WorkItemResponse>.Success(ToResponse(item));
    }

    private static WorkItemResponse ToResponse(WorkItem item) =>
        new(item.Id, item.Title, item.Description, item.Status, item.CreatedAt);
}
